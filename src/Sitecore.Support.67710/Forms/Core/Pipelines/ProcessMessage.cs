namespace Sitecore.Support.Forms.Core.Pipelines
{
  using System;
  using System.Net.Mail;
  using System.Text.RegularExpressions;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Links;
  using Sitecore.StringExtensions;
  using Sitecore.WFFM.Abstractions.Actions;
  using Sitecore.WFFM.Abstractions.Dependencies;
  using Sitecore.WFFM.Abstractions.Mail;
  using Sitecore.WFFM.Abstractions.Shared;
  using Sitecore.WFFM.Abstractions.Utils;

  public class ProcessMessage
  {
    #region Fields

    private readonly string srcReplacer;
    private readonly string shortHrefReplacer;
    private readonly string shortHrefMediaReplacer;
    private readonly string hrefReplacer;

    #endregion

    #region Properties

    public IItemRepository ItemRepository { get; set; }
    public IFieldProvider FieldProvider { get; set; }

    #endregion

    #region Constructors

    public ProcessMessage() : this(DependenciesManager.WebUtil)
    {
    }

    public ProcessMessage(IWebUtil webUtil)
    {
      Assert.IsNotNull(webUtil, "webUtil");
      srcReplacer = string.Join(string.Empty, new[] { "src=\"", webUtil.GetServerUrl(), "/~" });
      shortHrefReplacer = string.Join(string.Empty, new[] { "href=\"", webUtil.GetServerUrl(), "/" });
      shortHrefMediaReplacer = string.Join(string.Empty, new[] { "href=\"", webUtil.GetServerUrl(), "/~/" });
      hrefReplacer = shortHrefReplacer + "~";
    }

    #endregion

    #region Methods

    public void ExpandTokens(ProcessMessageArgs args)
    {
      Assert.IsNotNull(this.ItemRepository, "ItemRepository");
      Assert.IsNotNull(this.FieldProvider, "FieldProvider");

      foreach (AdaptedControlResult field in args.Fields)
      {
        var item = this.ItemRepository.CreateFieldItem(this.ItemRepository.GetItem(field.FieldID));

        string value = field.Value;
        value = this.FieldProvider.GetAdaptedValue(field.FieldID, value);
        value = Regex.Replace(value, "src=\"/sitecore/shell/themes/standard/~", this.srcReplacer);
        value = Regex.Replace(value, "href=\"/sitecore/shell/themes/standard/~", this.hrefReplacer);
        value = Regex.Replace(value, "on\\w*=\".*?\"", string.Empty);

        if (args.MessageType == MessageType.Sms)
        {
          args.Mail.Replace("[{0}]".FormatWith(item.FieldDisplayName), value);
          args.Mail.Replace("[{0}]".FormatWith(item.Name), value);
        }
        else
        {
          if (!string.IsNullOrEmpty(field.Parameters) && args.IsBodyHtml)
          {
            if (field.Parameters.StartsWith("multipleline"))
            {
              value = value.Replace(Environment.NewLine, "<br/>");
            }
            if (field.Parameters.StartsWith("secure") && field.Parameters.Contains("<schidden>"))
            {
              value = Regex.Replace(value, @"\d", "*");
            }
          }

          var replaced = args.Mail.ToString();

          if (Regex.IsMatch(replaced, "\\[<label id=\"" + item.ID + "\">[^<]+?</label>]"))
          {
            replaced = Regex.Replace(replaced, "\\[<label id=\"" + item.ID + "\">[^<]+?</label>]", value);
          }

          if (Regex.IsMatch(replaced, "\\[<label id=\"" + item.ID + "\" renderfield=\"Value\">[^<]+?</label>]"))
          {
            replaced = Regex.Replace(replaced, "\\[<label id=\"" + item.ID + "\" renderfield=\"Value\">[^<]+?</label>]", field.Value);
          }

          if (Regex.IsMatch(replaced, "\\[<label id=\"" + item.ID + "\" renderfield=\"Text\">[^<]+?</label>]"))
          {
            replaced = Regex.Replace(replaced, "\\[<label id=\"" + item.ID + "\" renderfield=\"Text\">[^<]+?</label>]", value);
          }

          replaced = replaced.Replace(item.ID.ToString(), value);
          args.Mail.Clear().Append(replaced);
        }


        args.From = args.From.Replace("[" + item.ID + "]", value);
        args.From = args.From.Replace(item.ID.ToString(), value);
        args.To.Replace(string.Join(string.Empty, new[] { "[", item.ID.ToString(), "]" }), value);
        args.To.Replace(string.Join(string.Empty, new[] { item.ID.ToString() }), value);
        args.CC.Replace(string.Join(string.Empty, new[] { "[", item.ID.ToString(), "]" }), value);
        args.CC.Replace(string.Join(string.Empty, new[] { item.ID.ToString() }), value);
        args.Subject.Replace(string.Join(string.Empty, new[] { "[", item.ID.ToString(), "]" }), value);

        args.From = args.From.Replace("[" + item.FieldDisplayName + "]", value);
        args.To.Replace(string.Join(string.Empty, new[] { "[", item.FieldDisplayName, "]" }), value);
        args.CC.Replace(string.Join(string.Empty, new[] { "[", item.FieldDisplayName, "]" }), value);
        args.Subject.Replace(string.Join(string.Empty, new[] { "[", item.FieldDisplayName, "]" }), value);

        args.From = args.From.Replace("[" + field.FieldName + "]", value);
        args.To.Replace(string.Join(string.Empty, new[] { "[", field.FieldName, "]" }), value);
        args.CC.Replace(string.Join(string.Empty, new[] { "[", field.FieldName, "]" }), value);
        args.Subject.Replace(string.Join(string.Empty, new[] { "[", field.FieldName, "]" }), value);
      }
    }

    #endregion
  }
}