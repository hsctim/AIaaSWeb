namespace AIaaS.Web.Areas.App.Models.Common.Modals
{
    public class ModalHeaderViewModel
    {
        public string Title { get; set; }

        public string Info { get; set; }

        public ModalHeaderViewModel(string title, string info = "")
        {
            Title = title;
            Info = info;
        }
    }
}
