﻿using System.Threading.Tasks;
using AIaaS.Views;
using Xamarin.Forms;

namespace AIaaS.Services.Modal
{
    public interface IModalService
    {
        Task ShowModalAsync(Page page);

        Task ShowModalAsync<TView>(object navigationParameter) where TView : IXamarinView;

        Task<Page> CloseModalAsync();
    }
}
