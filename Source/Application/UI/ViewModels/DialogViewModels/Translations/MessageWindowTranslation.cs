using Translatable;

namespace pdfforge.PDFCreator.UI.ViewModels.DialogViewModels.Translations
{
     public class MessageWindowTranslation : ITranslatable
     {
          public string Cancel { get; private set; } = "Cancel";
          public string MoreInfo { get; private set; } = "More information";
          public string No { get; private set; } = "No";
          public string Ok { get; private set; } = "OK";
          public string Retry { get; private set; } = "Retry";
          public string Yes { get; private set; } = "Yes";
     }
}
