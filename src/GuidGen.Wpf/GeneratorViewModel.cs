using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace GuidGen
{
    //TODO: SOLID principles dictate that this could probably be decoupled a bit further, data from view model, command patterns, and so on, but this will get the job done for now...
    public class GeneratorViewModel : ViewModelBase, IGeneratorOptions
    {
        #region GeneratorOptions Members

        //TODO: arguably, if we really wanted to, Current should be captured in a separate class, provider, or even 'options', per se, just apart from the view model...
        private Guid _current;

        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        public Guid Current
        {
            get { return _current; }
            private set
            {
                if (_current == value) return;
                _current = value;
                RaisePropertyChanged();
                RaisePropertyChanged("Formats");
                RaisePropertyChanged("SelectedResult");
            }
        }

        /// <summary>
        /// Case backing field.
        /// </summary>
        private TextCase _textCase;

        /// <summary>
        /// Gets the Case.
        /// </summary>
        public TextCase Case
        {
            get { return _textCase; }
            private set
            {
                if (value == _textCase) return;
                _textCase = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// SelectedFormat backing field.
        /// </summary>
        private FormatViewModel _selectedFormat;

        /// <summary>
        /// Gets or sets the SelectedFormat
        /// </summary>
        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        public FormatViewModel SelectedFormat
        {
            get { return _selectedFormat; }
            set
            {
                if (ReferenceEquals(value, _selectedFormat)) return;
                _selectedFormat = value;
                RaisePropertyChanged();
                RaisePropertyChanged("SelectedResult");
            }
        }

        #endregion

        /// <summary>
        /// Gets the Formats for use by the generator. This is the rest of the view model.
        /// </summary>
        public IEnumerable<IFormat> Formats { get; }

        /// <summary>
        /// Gets the SelectedResult for display purposes.
        /// This illustrates the power and flexibility of a loosely coupled MVVM.
        /// </summary>
        public string SelectedResult => SelectedFormat?.FormattedText.Replace("_", "__") ?? string.Empty;

        /// <summary>
        /// Gets the ResultHeader based on the <see cref="Case"/>.
        /// </summary>
        public string ResultHeader => $"Result ({Case.GetResultHeaderText()})";

        /// <summary>
        /// Default Constructor
        /// </summary>
        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        public GeneratorViewModel()
        {
            New();

            Case = TextCase.Lower;

            //No need to evaluate the listed formats twice.
            Formats = GetFormats(this).ToList();

            var number = 0;

            //The good 'old' foreach loop still has its place.
            foreach (var f in Formats)
            {
                f.Number = ++number;
                //TODO: TBD: could potentially get fancier with the PC event, but does not seem to be necessary
                f.PropertyChanged += (s, e) => RaisePropertyChanged("SelectedResult");
            }
        }

        private ICommand _newCommand;

        public ICommand NewCommand
        {
            get
            {
                return _newCommand
                       ?? (_newCommand = new DelegateCommand(x => New(), x => true));
            }
        }

        private ICommand _copyCommand;

        public ICommand CopyCommand
        {
            get
            {
                return _copyCommand
                       ?? (_copyCommand = new DelegateCommand(x => Copy(), x => true));
            }
        }

        private ICommand _caseCommand;

        public ICommand CaseCommand
        {
            get
            {
                return _caseCommand
                       ?? (_caseCommand = new DelegateCommand(x => ToggleCase(), x => true));
            }
        }

        public void New()
        {
            Current = Guid.NewGuid();
        }

        /// <summary>
        /// Gets the CaseCommandContent content.
        /// </summary>
        public string CaseCommandContent
        {
            get { return string.Format("To _{0} Case", Case.GetNextTextCase()); }
        }

        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        public void ToggleCase()
        {
            Case = Case.GetNextTextCase();
            RaisePropertyChanged("CaseCommandContent");
            RaisePropertyChanged("ResultHeader");
            RaisePropertyChanged("SelectedResult");
        }

        public void Copy()
        {
            //TODO: which would then also potentially be allowing the view model to present itself for copy to the clipboard without needing to call any methods
            if (SelectedFormat == null) return;
            Clipboard.SetText(SelectedFormat.FormattedText);
        }

        private static IEnumerable<IFormat> GetFormats(IGeneratorOptions options)
        {
            //TODO: TBD: from here's it's not far to drop it in as a VERY loosely coupled resource in App, MainWindow, etc
            var interfaceType = typeof (IFormat);

            var assemblyTypes = interfaceType.Assembly.GetTypes().ToArray();

            //Do not order the types themselves.
            var viewModelTypes = assemblyTypes.Where(
                t => t.IsPublic && t.IsClass && !t.IsAbstract
                     && interfaceType.IsAssignableFrom(t))
                .ToArray();

            var formatters = viewModelTypes.Select(t =>
            {
                var ctor = t.GetConstructor(new[] {typeof (IGeneratorOptions)});
                Debug.Assert(ctor != null);
                var instance = ctor.Invoke(new object[] {options});
                Debug.Assert(instance != null);
                return (IFormat) instance;
            });

            /* Instead order the formatter instances. That'll work pretty well. Actually,
             * I'm kind of surprised that the extension method works here, considering there
             * is a Generic Constraint on its TFormat, wanting a class that implements IFormat,
             * not just the simple IFormat, per se. I'm not saying 'no', just surprised, is all. */

            var ordered = formatters.OrderBy(f => f.GetDisplayOrder().Order).ToArray();

            return ordered;
        }
    }
}
