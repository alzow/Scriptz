namespace QueueApp.Features.OperatorQueue.Models;

// What the board is allowed to offer next. complete_entry accepts a waiting entry, so nothing on
// the server stops an entry going straight from the line to done — with serving_at and done_at
// stamped milliseconds apart, which is the shop's own record of how long its jobs take. The sheet
// is where that is closed: a waiting entry is only ever offered the start, a serving one the end.
public enum EntryStage
{
    Waiting,
    Serving,
}
