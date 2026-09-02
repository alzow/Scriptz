using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Shared.Domain;

// business_queue_summary returns every active operator, available or not, because the board wants
// the off-shift ones on it too. Every customer-facing "shortest wait" read has to drop them first:
// an operator who went home has nobody waiting and reads 0 minutes, which is the most attractive
// number on the screen and the least true.
//
// The ordering here is the same one join_queue applies in SQL — shortest projected wait, then
// fewest waiting, then the shop's own display order. Two copies of a rule is one too many, but the
// app has to predict the pick before the row exists, so the copies at least have to agree.
public static class QueueSummaryExtensions
{
    public static IEnumerable<QueueSummaryRow> OnShift(this IEnumerable<QueueSummaryRow> summary) =>
        summary.Where(r => r.OperatorId.HasValue && r.IsAvailable);

    // The SQL's third key is sort_order, and business_queue_summary already returns its rows in it.
    // LINQ's OrderBy is stable, so arrival order supplies that key without a column to sort on —
    // QueueSummaryRow has no SortOrder of its own.
    public static QueueSummaryRow? FastestOperator(this IEnumerable<QueueSummaryRow> summary) =>
        summary
            .OnShift()
            .OrderBy(r => r.NewJoinWaitMinutes)
            .ThenBy(r => r.WaitingCount)
            .FirstOrDefault();

    // Null when the shop has nobody on shift — the wait is then genuinely unknowable rather than
    // zero, and every caller has to say so rather than print a confident 0.
    public static double? FastestWaitMinutes(this IEnumerable<QueueSummaryRow> summary) =>
        summary.FastestOperator()?.NewJoinWaitMinutes;
}
