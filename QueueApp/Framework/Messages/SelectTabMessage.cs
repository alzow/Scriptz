namespace QueueApp.Framework.Messages;

// Sent by a page inside the modal over the tabs when the tabs underneath should land on a specific
// tab. It has to go through the messenger: MPowerKit resolves SelectTab against the tabbed page
// above the navigation service's own page, and a modal is its own root on the window's modal stack,
// so nothing inside one has a TabbedPage over it to select on. Only the tabbed page's own
// navigation service does.
public record SelectTabMessage(string TabName);
