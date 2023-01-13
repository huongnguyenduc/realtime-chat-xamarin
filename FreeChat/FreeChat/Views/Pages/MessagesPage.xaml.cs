using FreeChat.Helpers;
using FreeChat.Helpers.MyEventArgs;
using FreeChat.PlatformSpecifics;
using FreeChat.ViewModels;
using Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FreeChat.Views.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MessagesPage : BasePage
    {
        #region BackCommand
        public static readonly BindableProperty BackCommandProperty = BindableProperty.Create(nameof(BackCommand), typeof(ICommand), typeof(MessagesPage), propertyChanged: (obj, old, newV) =>
        {
            var me = obj as MessagesPage;
            if (newV != null && !(newV is ICommand)) return;
            var oldBackCommand = (ICommand)old;
            var newBackCommand = (ICommand)newV;
            me?.BackCommandChanged(oldBackCommand, newBackCommand);
        });

        private void BackCommandChanged(ICommand oldBackCommand, ICommand newBackCommand)
        {

        }

        /// <summary>
        /// When back button is pressed, this command is fired
        /// </summary>
        public ICommand BackCommand
        {
            get => (ICommand)GetValue(BackCommandProperty);
            set => SetValue(BackCommandProperty, value);
        }
        #endregion

        void IsFocusOnKeyBoardChanged(bool newIsFocusOnKeyBoard)
        {
            if (newIsFocusOnKeyBoard)
                TextInput.Focus();
            else
                TextInput.Unfocus();
        }

        public MessagesPage()
        {
            InitializeComponent();
            BackCommand = new Command(async _ =>
                await AppShell.Current.Navigation.PopModalAsync());
            TextInput.Focused += TextInput_Focused;
        }

        private void TextInput_Focused(object sender, FocusEventArgs e)
        {
            if (!e.IsFocused)
                VisualStateManager.GoToState(TextInput, "Unfocused");
        }

        protected override void OnAppearing()
        {
            MessagingCenter.Subscribe<IViewModel, MyFocusEventArgs>(this, Constants.ShowKeyboard, (s, args) =>
                IsFocusOnKeyBoardChanged(args.IsFocused));

            MessagingCenter.Subscribe<IViewModel, ScrollToItemEventArgs>(this, Constants.ScrollToItem, async (s, eargs) =>
            {
                ScrollToView((Message)eargs.Item, 5);
            });
            MessagingCenter.Subscribe<object, KeyboardAppearEventArgs>(this, Constants.iOSKeyboardAppears, (sender, eargs) =>
            {
                if (MessagesGrid.TranslationY == 0)
                {
                    MessagesGrid.TranslationY -= eargs.KeyboardSize;
                }
            });
            MessagingCenter.Subscribe<object, string>(this, Constants.iOSKeyboardDisappears, (sender, eargs) =>
            {
                MessagesGrid.TranslationY = 0;
            });
            base.OnAppearing();
        }

        async void ScrollToView(Message item, int count)
        {
            if (count <= 0) return;
            ObservableCollection<ViewModels.Helpers.MessagesGroup> Messages = (ObservableCollection<ViewModels.Helpers.MessagesGroup>)MessagesCollectionView.ItemsSource;
            if (Messages != null && Messages.Count > 0 && Messages.Last().Count() > 0 && Messages.Last().Contains(item))
            {
                await Task.Delay(TimeSpan.FromSeconds(0.6));
                MessagesCollectionView.ScrollTo(item);
            }
            else
            {

                ScrollToView(item, count - 1);
            }


        }

        protected override void OnDisappearing()
        {
            MessagingCenter.Unsubscribe<IViewModel, MyFocusEventArgs>(this, Constants.ShowKeyboard);
            MessagingCenter.Unsubscribe<IViewModel, ScrollToItemEventArgs>(this, Constants.ScrollToItem);
            MessagingCenter.Unsubscribe<object, KeyboardAppearEventArgs>(this, Constants.iOSKeyboardAppears);
            base.OnDisappearing();
        }
    }
}