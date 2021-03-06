﻿using CharacterMap.Controls;
using CharacterMap.Core;
using CharacterMap.Helpers;
using CharacterMap.Services;
using CharacterMap.ViewModels;
using CharacterMap.Views;
using CommonServiceLocator;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CharacterMap.Helpers
{
    public static class FlyoutHelper
    {
        private static UserCollectionsService _collections { get; } = ServiceLocator.Current.GetInstance<UserCollectionsService>();

        public static void CreateMenu(
            MenuFlyout menu,
            InstalledFont font,
            bool standalone,
            CreateCollectionDialog createDialog)
        {
            MainViewModel main = null;
            if (ResourceHelper.TryGet("Locator", out ViewModelLocator l))
                main = l.Main;

            void OpenInNewWindow(object s, RoutedEventArgs args)
            {
                if (s is FrameworkElement f && f.Tag is InstalledFont fnt)
                _ = FontMapView.CreateNewViewForFontAsync(fnt);
            }

            async void AddToSymbolFonts_Click(object sender, RoutedEventArgs e)
            {
                if (sender is FrameworkElement f && f.DataContext is InstalledFont fnt)
                {
                    await _collections.AddToCollectionAsync(
                                   fnt, _collections.SymbolCollection);

                    Messenger.Default.Send(new CollectionsUpdatedMessage());
                }
            }

            void CreateCollection_Click(object sender, RoutedEventArgs e)
            {
                createDialog.TemplateSettings.CollectionTitle = null;
                createDialog.DataContext = (sender as FrameworkElement)?.DataContext;
                _ = createDialog.ShowAsync();
            }

            async void RemoveFrom_Click(object sender, RoutedEventArgs e)
            {
                if (sender is FrameworkElement f && f.DataContext is InstalledFont fnt)
                {
                    UserFontCollection collection = (main.SelectedCollection == null && main.FontListFilter == 1)
                        ? _collections.SymbolCollection
                        : main.SelectedCollection;

                    await _collections.RemoveFromCollectionAsync(fnt, collection);
                    Messenger.Default.Send(new CollectionsUpdatedMessage());

                }
            }

            void RemoveMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
            {
                if (sender is MenuFlyoutItem item && item.Tag is InstalledFont fnt)
                {
                    _ = MainPage.MainDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        main.TryRemoveFont(fnt);
                    });
                }
            }

            menu.Items.Clear();
            MenuFlyoutSubItem coll = null;

            {  // HORRIBLE Hacks, because MenuFlyoutSubItem never updates it's UI tree after the first
               // render, meaning we can't dynamically update items. Instead we need to make an entirely
               // new one.

                // Add "Open in New Window" button
                if (!standalone)
                {
                    var newWindow = new MenuFlyoutItem
                    {
                        Text = Localization.Get("OpenInNewWindow/Text"),
                        Icon = new SymbolIcon { Symbol = Symbol.NewWindow },
                        Tag = font
                    };
                    newWindow.Click += OpenInNewWindow;
                    menu.Items.Add(newWindow);
                }
                
                // Add "Delete Font" button
                if (!standalone)
                {
                    if (font.HasImportedFiles)
                    {
                        var removeFont = new MenuFlyoutItem
                        {
                            Text = Localization.Get("RemoveFontFlyout/Text"),
                            Icon = new SymbolIcon { Symbol = Symbol.Delete },
                            Tag = font
                        };
                        removeFont.Click += RemoveMenuFlyoutItem_Click;
                        menu.Items.Add(removeFont);
                    }
                }
                

                // Add "Add to Collection" button
                MenuFlyoutSubItem newColl = new MenuFlyoutSubItem
                {
                    Text = Localization.Get("AddToCollectionFlyout/Text"),
                    Icon = new SymbolIcon { Symbol = Symbol.AllApps }
                };

                // Create "New Collection" Item
                var newCollection = new MenuFlyoutItem
                {
                    Text = Localization.Get("NewCollectionItem/Text"),
                    Icon = new SymbolIcon
                    {
                        Symbol = Symbol.Add
                    }
                };
                newCollection.Click += CreateCollection_Click;
                newColl.Items.Add(newCollection);

                // Create "Symbol Font" item
                if (!font.IsSymbolFont)
                {
                    newColl.Items.Add(new MenuFlyoutSeparator());

                    var symb = new MenuFlyoutItem
                    {
                        Text = Localization.Get("OptionSymbolFonts/Text"),
                        IsEnabled = !_collections.SymbolCollection.Fonts.Contains(font.Name)
                    };
                    symb.Click += AddToSymbolFonts_Click;
                    newColl.Items.Add(symb);
                }

                coll = newColl;
                menu.Items.Add(coll);
            }

            // Only show the "Remove from Collection" menu item if:
            //  -- we are not in a standalone window
            //  AND
            //  -- we are in a custom collection
            //  OR 
            //  -- we are in the Symbol Font collection, and this is a font that 
            //     the user has manually tagged as a symbol font
            if (!standalone)
            {
                if (main.SelectedCollection != null ||
                    (main.FontListFilter == 1 && !font.FontFace.IsSymbolFont))
                {
                    var removeItem = new MenuFlyoutItem
                    {
                        Text = Localization.Get("RemoveFromCollectionItem/Text"),
                        Icon = new SymbolIcon { Symbol = Symbol.Remove },
                        Tag = font
                    };
                    removeItem.Click += RemoveFrom_Click;
                    menu.Items.Add(removeItem);
                }
            }
            

            // Add items for each user Collection
            if (_collections.Items.Count > 0)
            {
                coll.Items.Add(new MenuFlyoutSeparator());

                foreach (var item in _collections.Items)
                {
                    var m = new MenuFlyoutItem { DataContext = item, Text = item.Name, IsEnabled = !item.Fonts.Contains(font.Name) };
                    if (m.IsEnabled)
                    {
                        m.Click += async (s, a) =>
                        {
                            await _collections.AddToCollectionAsync(
                                font, (UserFontCollection)(((FrameworkElement)s).DataContext));
                        };
                    }
                    coll.Items.Add(m);
                }
            }
        }

    }
}
