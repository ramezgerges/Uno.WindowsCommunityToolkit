// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Newtonsoft.Json;
using Windows.ApplicationModel;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Navigation;

namespace Microsoft.Toolkit.Uwp.SampleApp.Pages
{
    public sealed partial class About : INotifyPropertyChanged
    {
        private Compositor _compositor;

        private IEnumerable<Sample> _recentSamples;

        public IEnumerable<Sample> RecentSamples
        {
            get
            {
                return _recentSamples;
            }

            set
            {
                _recentSamples = value;
                OnPropertyChanged();
            }
        }

        private static List<Sample> _newSamples;

        public List<Sample> NewSamples
        {
            get
            {
                return _newSamples;
            }

            set
            {
                _newSamples = value;
                OnPropertyChanged();
            }
        }

        private List<GitHubRelease> _githubReleases;

        public List<GitHubRelease> GitHubReleases
        {
            get
            {
                return _githubReleases;
            }

            set
            {
                _githubReleases = value;
                OnPropertyChanged();
            }
        }

        private static LandingPageLinks _landingPageLinks;

        public LandingPageLinks LandingPageLinks
        {
            get
            {
                return _landingPageLinks;
            }

            set
            {
                _landingPageLinks = value;
                OnPropertyChanged();
            }
        }

        public About()
        {
            InitializeComponent();
        }

        public static Visibility VisibleIfCollectionEmpty(IEnumerable<Sample> collection)
        {
            return collection != null && collection.Count() > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Shell.Current.SetAppTitle("About");

            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

            var t = Init();

            Windows.UI.Xaml.Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

			Console.WriteLine("<- About: OnNavigatedTo");
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            Windows.UI.Xaml.Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            var keyChar = (char)args.VirtualKey;
            if (char.IsLetterOrDigit(keyChar))
            {
                Shell.Current.StartSearch(keyChar.ToString());
            }
        }

        private async Task Init()
        {
			try
			{
				var loadDataTask = UpdateSections();
				var recentSamplesTask = Samples.GetRecentSamples();
				var gitHubTask = Data.GitHub.GetPublishedReleases();

				await Task.WhenAll(loadDataTask, recentSamplesTask, gitHubTask);

				RecentSamples = recentSamplesTask.Result;
				GitHubReleases = gitHubTask.Result;

				var counter = 1;
				var delay = 70;

				foreach (var child in InnerGrid.Children)
				{
					if (child is ItemsControl itemsControl)
					{
						foreach (var childOfChild in itemsControl.Items)
						{
							Implicit.GetShowAnimations((UIElement)childOfChild).Add(new OpacityAnimation()
							{
								From = 0,
								To = 1,
								Duration = TimeSpan.FromMilliseconds(300),
								Delay = TimeSpan.FromMilliseconds(counter++ * delay),
								SetInitialValueBeforeDelay = true
							});
						}
					}
					else
					{
#if NETFX_CORE
                   Implicit.GetShowAnimations(child).Add(new OpacityAnimation()
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(300),
                        Delay = TimeSpan.FromMilliseconds(counter++ * delay),
                        SetInitialValueBeforeDelay = true
                    });
#endif
					}
				}
				

				Root.Visibility = Visibility.Visible;
			}
			catch(Exception e)
			{
				Console.WriteLine($"About init failed: {e}");
			}
        }

        private void RecentSample_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as HyperlinkButton;
            if (button.DataContext is Sample sample)
            {
#if NETFX_CORE // UNO TODO
               TrackingManager.TrackEvent("LandingPageRecentClick", sample.Name);
#endif
				Shell.Current.NavigateToSample(sample);
            }
        }

        private void NewSample_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as HyperlinkButton;
            if (button.DataContext is Sample sample)
            {
#if NETFX_CORE // UNO TODO
                TrackingManager.TrackEvent("LandingPageNewClick", sample.Name);
#endif

				Shell.Current.NavigateToSample(sample);
            }
        }

        private void ReleaseNotes_Click(object sender, RoutedEventArgs e)
        {
#if NETFX_CORE // UNO TODO
            var button = sender as HyperlinkButton;
            if (button.DataContext is GitHubRelease release)
            {
                TrackingManager.TrackEvent("LandingPageReleaseClick", release.Name);
            }
#endif
		}

		private void Link_Clicked(object sender, RoutedEventArgs e)
        {
#if NETFX_CORE // UNO TODO
           var button = sender as HyperlinkButton;
            if (button.Content is TextBlock textBlock)
            {
                TrackingManager.TrackEvent("LandingPageLinkClick", textBlock.Text);
            }
#endif
		}

		private async Task UpdateSections()
        {
            if (LandingPageLinks == null)
            {
                using (var jsonStream = await StreamHelper.GetEmbeddedFileStreamAsync(GetType(), "landingPageLinks.json"))
                {
                    var jsonString = await jsonStream.ReadTextAsync();
                    var links = JsonConvert.DeserializeObject<LandingPageLinks>(jsonString);
                    var packageVersion = Package.Current.Id.Version;

                    var resource = links.Resources.FirstOrDefault(item => item.ID == "app");
                    if (resource != null)
                    {
                        resource.Links[0].Title = $"Version {packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}";
                    }

                    LandingPageLinks = links;
                }

                var samples = new List<Sample>();

                foreach (var newSample in LandingPageLinks.NewSamples)
                {
                    var sample = await Samples.GetSampleByName(newSample);
                    if (sample != null)
                    {
                        samples.Add(sample);
                    }
                }

                NewSamples = samples;
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Shell.Current.AttachScroll(Scroller);
        }
    }
}