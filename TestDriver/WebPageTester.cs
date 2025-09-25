using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using HtmlAgilityPack;

namespace Gb18030.TestDriver
{
    public class WebPageTester
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _encodingName;
        private readonly string[] _allStrings;
        private readonly string _overrideString;
        private readonly bool _verbose;
        private string _lastHtml;

        public string LastHtml => _lastHtml;

        public WebPageTester(HttpClient http, string baseUrl, string encodingName, string[] allStrings, string overrideString = null, bool verbose = false)
        {
            _http = http;
            _baseUrl = baseUrl.TrimEnd('/');
            _encodingName = encodingName;
            _allStrings = allStrings;
            _overrideString = overrideString;
            _verbose = verbose;
        }

        public string[] PagesToTest => new[] { "BasicControls.aspx", "DataControls.aspx" };

        public IEnumerable<ControlTestResult> TestPage(string page, int index, string expected)
        {
            var url = $"{_baseUrl}/{page}?i={index}";
            if (!string.IsNullOrEmpty(_overrideString)) url += "&s=" + Uri.EscapeDataString(_overrideString);
            if (_verbose) Console.WriteLine("REQUEST: " + url);
            if (!string.IsNullOrEmpty(_encodingName)) url += "&enc=" + Uri.EscapeDataString(_encodingName);
            var bytes = _http.GetByteArrayAsync(url).GetAwaiter().GetResult();
            // Determine encoding (simple: use header if available else UTF-8)
            // For now just decode as UTF-8 or specified encoding if provided
            Encoding enc = Encoding.UTF8;
            if (!string.IsNullOrEmpty(_encodingName))
            {
                try { enc = Encoding.GetEncoding(_encodingName); } catch { }
            }
            var html = enc.GetString(bytes);
            _lastHtml = html; // store raw HTML so caller can optionally output it
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var pageControls = GetControlsForPage(page);
            foreach (var controlId in pageControls)
            {
                var result = new ControlTestResult
                {
                    Page = page,
                    ControlId = controlId,
                    StringIndex = index,
                    Expected = ExpectedFor(controlId, index, expected)
                };
                try
                {
                    result.Actual = HttpUtility.HtmlDecode(ExtractControlValue(doc, controlId));
                    result.Passed = string.Equals(result.Actual, result.Expected, StringComparison.Ordinal);
                    if (!result.Passed)
                        result.Message = "Mismatch";
                }
                catch (Exception ex)
                {
                    result.Passed = false;
                    result.Message = ex.GetType().Name + ": " + ex.Message;
                }
                yield return result;
            }
        }

        private string[] GetControlsForPage(string page)
        {
            if (page.StartsWith("BasicControls", StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    // Base
                    "LabelTest","LiteralTestContainer","TextBoxTest","HyperLinkTest","LinkButtonTest","ButtonTest","CheckBoxTest","RadioButtonTest","DropDownListTest","ListBoxTest","BulletedListTest",
                    // Synthetic attributes / extra items
                    "TextBoxTest.placeholder","LabelTest.title","HyperLinkTest.title","LinkButtonTest.title","ButtonTest.title","DropDownListTest.title","ListBoxTest.title","BulletedListTest.title","CheckBoxTest.title","RadioButtonTest.title",
                    "DropDownListTest.item2","ListBoxTest.item2"
                };
            }
            if (page.StartsWith("DataControls", StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    "GridViewTest","RepeaterTest","DataListTest","ListViewTest","DetailsViewTest","FormViewTest",
                    "GridViewTest.header","GridViewTest.caption","DetailsViewTest.header","RepeaterTest.header"
                };
            }
            return Array.Empty<string>();
        }

        private string ExtractControlValue(HtmlDocument doc, string id)
        {
            if (id.Contains('.'))
            {
                var parts = id.Split('.');
                var baseId = parts[0];
                var suffix = parts[1];
                switch (suffix)
                {
                    case "placeholder":
                        return GetAttributeOrAncestor(doc, baseId, "placeholder");
                    case "title":
                        return GetAttributeOrAncestor(doc, baseId, "title");
                    case "item2":
                        return GetNthListItem(doc, baseId, 2);
                    case "header":
                        if (baseId == "GridViewTest") return GetTextById(doc, "GridViewTestHeader");
                        if (baseId == "DetailsViewTest") return GetTextById(doc, "DetailsViewTest_DetailsViewTestHeader");
                        if (baseId == "RepeaterTest") return GetTextById(doc, "RepeaterTest_RepeaterHeader");
                        break;
                    case "caption":
                        if (baseId == "GridViewTest")
                        {
                            var table = doc.GetElementbyId("GridViewTest");
                            if (table != null)
                            {
                                var cap = table.Descendants("caption").FirstOrDefault();
                                if (cap != null) return cap.InnerText.Trim();
                            }
                        }
                        break;
                }
                throw new Exception("Synthetic surface not found: " + id);
            }

            // Some controls render differently
            if (id == "GridViewTest" || id == "DetailsViewTest") return ExtractTableCell(doc, id);

            var node = doc.GetElementbyId(id);
            if (node == null && id == "RepeaterTest") node = GetNodeByName(doc, "span", "repeater-item");
            if (node == null && id == "ListViewTest") node = GetNodeByName(doc, "span", "listview-item");
            if (node == null)
            {
                throw new Exception("Element not found");
            }

            switch (node.Name.ToLowerInvariant())
            {
                case "input":
                    var type = node.GetAttributeValue("type", "").ToLowerInvariant();
                    if (type == "checkbox" || type == "radio")
                    {
                        // Prefer explicit <label for="id"> text
                        var label = FindLabelFor(doc, id);
                        if (!string.IsNullOrEmpty(label)) return label;
                        // Fallback: parent span inner text minus input/label markup
                        var parent = node.ParentNode;
                        if (parent != null)
                        {
                            var lblNode = parent.Descendants().FirstOrDefault(n => n.Name.Equals("label", StringComparison.OrdinalIgnoreCase) && n.GetAttributeValue("for", "") == id);
                            if (lblNode != null) return lblNode.InnerText.Trim();
                            return parent.InnerText.Trim();
                        }
                        return string.Empty;
                    }
                    return node.GetAttributeValue("value", "");
                case "select":
                    var opt = node.Descendants("option").FirstOrDefault();
                    return opt?.InnerText.Trim();
                case "ul":
                case "ol":
                    return node.Descendants("li").FirstOrDefault()?.InnerText.Trim();
                case "span":
                case "div":
                case "a":
                case "button":
                    return node.InnerText.Trim();
                default:
                    return node.InnerText.Trim();
            }
        }

        private string ExtractTableCell(HtmlDocument doc, string id)
        {
            // Find table with id
            var table = doc.GetElementbyId(id);
            if (table == null) throw new Exception("GridView table not found");
            var cell = table.Descendants("td").FirstOrDefault();
            return cell?.InnerText.Trim();
        }

        private string ExpectedFor(string controlId, int primaryIndex, string primaryExpected)
        {
            //if (!controlId.Contains('.'))
            //{
            //    if (controlId == "GridViewTest.header" || controlId == "DetailsViewTest.header")
            //    {
            //        return _allStrings[(primaryIndex + 1) % _allStrings.Length];
            //    }
            //    return primaryExpected;
            //}
            //if (controlId.EndsWith(".item2") || controlId.EndsWith(".header"))
            //{
            //    return _allStrings[(primaryIndex + 1) % _allStrings.Length];
            //}
            // placeholder, title, caption reuse primary
            return primaryExpected;
        }

        private string GetAttribute(HtmlDocument doc, string id, string attr)
        {
            var node = doc.GetElementbyId(id);
            if (node == null) throw new Exception("Element not found for attribute: " + id);
            return node.GetAttributeValue(attr, "");
        }

        private string GetAttributeOrAncestor(HtmlDocument doc, string id, string attr)
        {
            var node = doc.GetElementbyId(id);
            if (node == null) throw new Exception("Element not found for attribute: " + id);
            var cur = node;
            for (int depth = 0; cur != null && depth < 4; depth++)
            {
                var val = cur.GetAttributeValue(attr, null);
                if (!string.IsNullOrEmpty(val)) return val;
                cur = cur.ParentNode;
            }
            return string.Empty; // attribute not found in ancestor chain
        }

        private HtmlNode GetNodeByName(HtmlDocument doc, string name, string className = null)
        {
            var nodes = doc.DocumentNode.Descendants(name);

            if (className != null)
                return nodes.FirstOrDefault(n => n.HasClass(className));
            return nodes.FirstOrDefault();
        }

        private string FindLabelFor(HtmlDocument doc, string id)
        {
            var label = doc.DocumentNode.Descendants("label").FirstOrDefault(l => l.GetAttributeValue("for", "") == id);
            return label?.InnerText.Trim();
        }

        private string GetNthListItem(HtmlDocument doc, string id, int n)
        {
            var node = doc.GetElementbyId(id);
            if (node == null) throw new Exception("List control not found: " + id);
            if (node.Name == "select")
            {
                var opts = node.Descendants("option").ToList();
                if (opts.Count >= n) return opts[n - 1].InnerText.Trim();
            }
            else if (node.Name == "ul" || node.Name == "ol")
            {
                var lis = node.Descendants("li").ToList();
                if (lis.Count >= n) return lis[n - 1].InnerText.Trim();
            }
            throw new Exception("Nth item not found: " + n);
        }

        private string GetTextById(HtmlDocument doc, string id)
        {
            var node = doc.GetElementbyId(id);
            if (node == null) throw new Exception("Element not found: " + id);
            return node.InnerText.Trim();
        }
    }
}