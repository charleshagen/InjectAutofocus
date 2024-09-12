using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.Utility;
using NINA.Core.Utility;
using CharlesHagen.NINA.InjectAutofocus;

namespace CharlesHagen.NINA.InjectAutofocus.InjectAutofocusTestCategory; 
/// <summary>
/// This Class shows the basic principle on how to add a new Sequence Trigger to the N.I.N.A. sequencer via the plugin interface
/// For ease of use this class inherits the abstract SequenceTrigger which already handles most of the running logic, like logging, exception handling etc.
/// A complete custom implementation by just implementing ISequenceTrigger is possible too
/// The following MetaData can be set to drive the initial values
/// --> Name - The name that will be displayed for the item
/// --> Description - a brief summary of what the item is doing. It will be displayed as a tooltip on mouseover in the application
/// --> Icon - a string to the key value of a Geometry inside N.I.N.A.'s geometry resources
///
/// If the item has some preconditions that should be validated, it shall also extend the IValidatable interface and add the validation logic accordingly.
/// </summary>
[ExportMetadata("Name", "Inject Autofocus")]
[ExportMetadata("Description", "This trigger will run Autofocus after the inject autofocus button is pressed at the next available opportunity")]
[ExportMetadata("Icon", "Inject_Autofocus_SVG")]
[ExportMetadata("Category", "Inject Autofocus")]
[Export(typeof(ISequenceTrigger))]
[JsonObject(MemberSerialization.OptIn)]
public class InjectAutofocusTrigger : SequenceTrigger {
    private IProfileService profileService;

    private IImageHistoryVM history;
    private ICameraMediator cameraMediator;
    private IFilterWheelMediator filterWheelMediator;
    private IFocuserMediator focuserMediator;
    private IAutoFocusVMFactory autoFocusVMFactory;

    /// <summary>
    /// The constructor marked with [ImportingConstructor] will be used to import and construct the object
    /// General device interfaces can be added to the constructor parameters and will be automatically injected on instantiation by the plugin loader
    /// </summary>
    /// <remarks>
    /// Available interfaces to be injected:
    ///     - IProfileService,
    ///     - ICameraMediator,
    ///     - ITelescopeMediator,
    ///     - IFocuserMediator,
    ///     - IFilterWheelMediator,
    ///     - IGuiderMediator,
    ///     - IRotatorMediator,
    ///     - IFlatDeviceMediator,
    ///     - IWeatherDataMediator,
    ///     - IImagingMediator,
    ///     - IApplicationStatusMediator,
    ///     - INighttimeCalculator,
    ///     - IPlanetariumFactory,
    ///     - IImageHistoryVM,
    ///     - IDeepSkyObjectSearchVM,
    ///     - IDomeMediator,
    ///     - IImageSaveMediator,
    ///     - ISwitchMediator,
    ///     - ISafetyMonitorMediator,
    ///     - IApplicationMediator
    ///     - IApplicationResourceDictionary
    ///     - IFramingAssistantVM
    ///     - IList<IDateTimeProvider>
    /// </remarks>
    [ImportingConstructor]
    public InjectAutofocusTrigger(IProfileService profileService, IImageHistoryVM history, ICameraMediator cameraMediator, IFilterWheelMediator filterWheelMediator, IFocuserMediator focuserMediator, IAutoFocusVMFactory autoFocusVMFactory) : base() {
        this.history = history;
        this.profileService = profileService;
        this.cameraMediator = cameraMediator;
        this.filterWheelMediator = filterWheelMediator;
        this.focuserMediator = focuserMediator;
        this.autoFocusVMFactory = autoFocusVMFactory;

        TriggerRunner.Add(new RunAutofocus(profileService, history, cameraMediator, filterWheelMediator, focuserMediator, autoFocusVMFactory));
    }

    private InjectAutofocusTrigger(InjectAutofocusTrigger cloneMe) : this(cloneMe.profileService, cloneMe.history, cloneMe.cameraMediator, cloneMe.filterWheelMediator, cloneMe.focuserMediator, cloneMe.autoFocusVMFactory) {
        CopyMetaData(cloneMe);
    }

    public override object Clone() {
        return new InjectAutofocusTrigger(this) {
            TriggerRunner = (SequentialContainer)TriggerRunner.Clone()
        };
    }

    /// <summary>
    /// The actual running logic for when the trigger should run
    /// </summary>
    /// <param name="context"></param>
    /// <param name="progress"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
        await TriggerRunner.Run(progress, token);
    }

    /// <summary>
    /// This method will be evaluated to see if the trigger should be executed.
    /// When true - the Execute method will be called
    /// Skipped otherwise
    ///
    /// For this example the trigger will fire when the random number generator generates an even number
    /// </summary>
    /// <param name="previousItem"></param>
    /// <param name="nextItem"></param>
    /// <returns></returns>
    public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
        var queuedFocus = TriggerState.GetTriggerState() > 0;
        if (!queuedFocus) return false;

        if (nextItem is not IExposureItem { ImageType: not "Light" }) return false;

        // If an autofocus has occured since the last request, do not focus again, reset flag
        if (history.AutoFocusPoints.LastOrDefault() is { } lastAF &&
            lastAF.AutoFocusPoint.Time > TriggerState.GetLastRequested()) { 
            queuedFocus = false; 
        }

        if (ItemUtility.IsTooCloseToMeridianFlip(Parent, TriggerRunner.GetItemsSnapshot().First().GetEstimatedDuration() + nextItem?.GetEstimatedDuration() ?? TimeSpan.Zero)) {
            Logger.Warning("Autofocus should be triggered, however the meridian flip is too close to be executed");
            // TODO: Check settings to see if meridian flip has AF enabled (do we want to clear the flag?)
            return false;
        }

        TriggerState.Reset();
        return queuedFocus;
    }

    /// <summary>
    /// This string will be used for logging
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return $"Category: {Category}, Item: {nameof(InjectAutofocusTrigger)}";
    }
}