using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Core.Enum;
using System.Windows.Input;

namespace CharlesHagen.NINA.InjectAutofocus.InjectAutofocusDockables; 
[Export(typeof(IDockableVM))]
public partial class InjectAutofocusDockable : DockableVM {
    private ISequenceMediator sequenceMediator;

    [ImportingConstructor]
    public InjectAutofocusDockable(
        IProfileService profileService,
        ISequenceMediator sequenceMediator) : base(profileService) {

        // This will reference the resource dictionary to import the SVG graphic and assign it as the icon for the header bar
        var dict = new ResourceDictionary();
        dict.Source = new Uri("CharlesHagen.NINA.InjectAutofocus;component/InjectAutofocusDockables/InjectAutofocusDockableTemplates.xaml", UriKind.RelativeOrAbsolute);
        ImageGeometry = (System.Windows.Media.GeometryGroup)dict["CharlesHagen.NINA.InjectAutofocusSVG"];
        ImageGeometry.Freeze();

        this.sequenceMediator = sequenceMediator;
        Title = "Inject Autofocus";
        Target = null;

        InjectAutofocusCommand = new RelayCommand(() => TriggerState.RequestAutofocus(), () => sequenceMediator.Initialized && sequenceMediator.IsAdvancedSequenceRunning());

        // Sequence mediator is not initialized when the plugin is loaded so we have to wait until we can register the handler.
        _ = Task.Run(async () => {
            while (!sequenceMediator.Initialized) {
                await Task.Delay(10);
            }
            sequenceMediator.SequenceStarting += SequenceStateChanged;
            sequenceMediator.SequenceFinished += SequenceStateChanged;
        });
    }

    public RelayCommand InjectAutofocusCommand { get; }

    private Task SequenceStateChanged(object sender, EventArgs e) {
        InjectAutofocusCommand.NotifyCanExecuteChanged();
        return Task.CompletedTask;
    }

    public void Dispose() {
        // On shutdown cleanup
    }
    public NighttimeData NighttimeData { get; private set; }
    public TelescopeInfo TelescopeInfo { get; private set; }
    public DeepSkyObject Target { get; private set; }

    public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
        // The IsVisible flag indicates if the dock window is active or hidden
        if (IsVisible) {
            TelescopeInfo = deviceInfo;
            if (TelescopeInfo.Connected && TelescopeInfo.TrackingEnabled && NighttimeData != null) {
                var showMoon = Target != null ? Target.Moon.DisplayMoon : false;
                if (Target == null || (Target?.Coordinates - deviceInfo.Coordinates)?.Distance.Degree > 1) {
                    Target = new DeepSkyObject("", deviceInfo.Coordinates, "", profileService.ActiveProfile.AstrometrySettings.Horizon);
                    Target.SetDateAndPosition(NighttimeCalculator.GetReferenceDate(DateTime.Now), profileService.ActiveProfile.AstrometrySettings.Latitude, profileService.ActiveProfile.AstrometrySettings.Longitude);
                    if (showMoon) {
                        Target.Refresh();
                        Target.Moon.DisplayMoon = true;
                    }
                    RaisePropertyChanged(nameof(Target));
                }
            } else {
                Target = null;
                RaisePropertyChanged(nameof(Target));
            }
            RaisePropertyChanged(nameof(TelescopeInfo));
        }
    }
}
