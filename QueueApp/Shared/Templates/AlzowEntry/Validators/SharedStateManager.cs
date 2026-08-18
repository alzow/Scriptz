using System;

namespace QueueApp.Shared.Templates.AlzowEntry.Validators;

public class SharedStateManager : ISharedStateManager
    {
        public  List<IValidationView> _alzowEntries = new List<IValidationView>();
        public event Action<bool> ValidationStateChanged;
        private VisualElement ParentElement { get; set; }
        public SharedStateManager(VisualElement parentElement = null)
        {
            ParentElement = parentElement;
        }

        public void RegisterEntry(IValidationView entry)
        {

            _alzowEntries.RemoveAll(i => ((AlzowEntry)i).PlaceholderText == ((AlzowEntry)entry).PlaceholderText);
            _alzowEntries.Add(entry);
            entry.ValidationChanged += OnEntryValidationStateChanged;
            NotifyStateChanged();

        }

        private void OnEntryValidationStateChanged(object sender, bool isValid)
        {
            NotifyStateChanged();
        }

        public void UnregisterEntry(IValidationView entry)
        {
            if (_alzowEntries.Contains(entry))
            {
                entry.ValidationChanged -= OnEntryValidationStateChanged;
                _alzowEntries.Remove(entry);
                NotifyStateChanged();
            }

        }

        private void NotifyStateChanged()
        {
            if (ParentElement != null && ParentElement.IsActuallyVisible() || ParentElement == null)
            {
                bool isValid = _alzowEntries.All(IsEntryValid);
                ValidationStateChanged?.Invoke(isValid);
            }
        }

        private bool IsEntryValid(IValidationView entry)
        {
            var inputView = entry as AlzowEntry;
            var allValid = inputView.IsValid || !inputView.IsVisible || (inputView.IsReadOnly && !string.IsNullOrEmpty(inputView.EntryText));
            return allValid;
        }

        public void ValidateOnViewUpdate()
        {
            NotifyStateChanged();
        }

        public void ResetValidationStatuses()
        {
            foreach(var entry in _alzowEntries)
            {
                (entry as AlzowEntry).ClearErrorState(true);
            }
        }
    }
