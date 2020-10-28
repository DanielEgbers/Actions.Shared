#r "nuget: Microsoft.SyndicationFeed.ReaderWriter, 1.0.2"

#nullable enable

using System.Xml;
using System.Xml.Linq;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;

public class FeedChannel
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Uri? Link { get; set; }
}

public class FeedItem
{
    public string? Link { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public string[] Categories { get; set; } = Array.Empty<string>();
    public string[] Contributors { get; set; } = Array.Empty<string>();
    public DateTimeOffset? LastUpdated { get; set; }
    public DateTimeOffset? Published { get; set; }
}

public static class Feed
{
    public static IEnumerable<string> ReadItemLinks(string feedXml)
    {
        try
        {
            var document = new XmlDocument();
            document.LoadXml(feedXml);
            return document.SelectNodes(".//item/link").Cast<XmlNode>().Select(n => n.InnerText);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public static async Task<IEnumerable<FeedItem>> ReadItemsAsync(string feedXml)
    {
        async Task<bool> ReadAsync(RssFeedReader reader)
        {
            try
            {
                return await reader.Read();
            }
            catch
            {
                return false;
            }
        }

        var items = new List<FeedItem>();

        using (var stringReader = new StringReader(feedXml))
        {
            using (var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings() { Async = true }))
            {
                var feedReader = new RssFeedReader(xmlReader);
                
                while (await ReadAsync(feedReader))
                {
                    if (feedReader.ElementType == SyndicationElementType.Item)
                    {
                        var synItem = await feedReader.ReadItem();

                        var item = new FeedItem()
                        {
                            Link = synItem.Links.FirstOrDefault()?.Uri?.ToString() ?? synItem.Id,
                            Title = synItem.Title,
                            Description = synItem.Description,
                            Categories = synItem.Categories?.Select(c => c.Name).ToArray() ?? Array.Empty<string>(),
                            Contributors = synItem.Contributors?.Select(c => c.Name).ToArray() ?? Array.Empty<string>(),
                            LastUpdated = synItem.LastUpdated,
                            Published = synItem.Published,
                        };

                        items.Add(item);
                    }
                }
            }
        }

        return items;
    }

    public static async Task<string> WriteAsync(FeedChannel channel, IEnumerable<FeedItem> items)
    {
        using (var stringWriter = new StringWriterWithEncoding(Encoding.UTF8))
        {
            using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Async = true, Indent = false }))
            {
                var feedWriter = new RssFeedWriter(xmlWriter);

                if (channel.Title != null)
                    await feedWriter.WriteTitle(channel.Title);

                if (channel.Description != null)
                    await feedWriter.WriteDescription(channel.Description);

                if (channel.Link != null)
                    await feedWriter.WriteValue("link", channel.Link.ToString());

                foreach (var item in items)
                {
                    var synItem = new SyndicationItem();
                    
                    if (item.Link != null)
                    {
                        var link = item.Link;

                        synItem.Id = link;

                        if (Uri.IsWellFormedUriString(link, UriKind.Absolute))
                            synItem.AddLink(new SyndicationLink(new Uri(link)));
                    }
                    
                    if (item.Title != null)
                        synItem.Title = item.Title;

                    if (item.LastUpdated.HasValue)
                        synItem.LastUpdated = item.LastUpdated.Value;

                    if (item.Published.HasValue)
                        synItem.Published = item.Published.Value;

                    if (!string.IsNullOrWhiteSpace(item.Image))
                        synItem.Description += $@"<img src=""{item.Image}"">";

                    if (!string.IsNullOrWhiteSpace(synItem.Description))
                        synItem.Description += "<br>";

                    if (!string.IsNullOrWhiteSpace(item.Description))
                        synItem.Description += item.Description;

                    await feedWriter.Write(synItem);
                }
            }

            return XDocument.Parse(stringWriter.ToString()).ToString();
        }
    }
 
    private class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding _encoding;

        public StringWriterWithEncoding(Encoding encoding)
        {
            _encoding = encoding;
        }

        public override Encoding Encoding => _encoding;
    }
}