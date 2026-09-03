using Microsoft.Maui.Controls.Shapes;
using QueueApp.Shared.Domain;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Features.BookingAgenda.Models;

public sealed record AgendaBuildRequest
{
    public required IReadOnlyList<AgendaBookingResponse> Bookings { get; init; }
    public required IReadOnlyList<AvailabilityBlockResponse> Blocks { get; init; }
    public required IReadOnlyList<SlotResponse> FreeSlots { get; init; }
    public required IReadOnlyDictionary<Guid, string> OperatorNames { get; init; }
    public required int ActiveOperatorCount { get; init; }
    public required string ResourcePluralNoun { get; init; }
    public required int ShortestServiceMinutes { get; init; }
    public required DateTimeOffset Now { get; init; }
}

public static class AgendaBuilder
{
    public static List<AgendaRow> Build(AgendaBuildRequest request)
    {
        var rows = new List<AgendaRow>();

        var live = request.Bookings
            .Where(b => !b.IsCancelled)
            .OrderBy(b => b.StartsAt)
            .ToList();

        foreach (var booking in live)
            rows.Add(BookingRow(booking, request.Now));

        var blockRanges = new List<(DateTimeOffset Start, DateTimeOffset End)>();
        rows.AddRange(BlockRows(request, blockRanges));

        var occupied = live
            .Select(b => (Start: b.LocalStart, End: b.LocalEnd))
            .Concat(blockRanges)
            .ToList();

        rows.AddRange(GapRows(request, occupied));

        rows.Sort(CompareRows);
        return rows;
    }

    public static int CompareRows(AgendaRow left, AgendaRow right)
    {
        var byStart = left.Start.CompareTo(right.Start);
        return byStart != 0 ? byStart : left.Kind.CompareTo(right.Kind);
    }

    public static AgendaRow BookingRow(AgendaBookingResponse booking, DateTimeOffset now)
    {
        var subtitleParts = new List<string>(2);
        if (booking.ServiceName.Length > 0) subtitleParts.Add(booking.ServiceName);
        if (booking.PriceText.Length > 0) subtitleParts.Add(booking.PriceText);

        var bar = AgendaPalette.Green;
        var background = AgendaPalette.Surface;
        var stroke = AgendaPalette.Line;
        var tagInk = AgendaPalette.Ink;
        var tagFill = Colors.Transparent;
        var titleInk = AgendaPalette.Ink;
        var timeInk = AgendaPalette.Ink;
        var subtitleInk = AgendaPalette.Muted;
        var metaInk = AgendaPalette.Dim;
        DoubleCollection? dash = null;
        var tag = string.Empty;

        var bayText = booking.Operator?.DisplayName.ToUpperInvariant() ?? string.Empty;
        var showMarkCollected = false;

        if (booking.IsAwaitingCollection)
        {
            bar = AgendaPalette.Purple;
            background = AgendaPalette.PurpleTint;
            stroke = AgendaPalette.PurpleBorder;
            tag = "READY FOR COLLECTION";
            tagInk = AgendaPalette.Purple;
            tagFill = AgendaPalette.PurpleTint;
            showMarkCollected = true;
            bayText = string.Empty;
        }
        else if (booking.IsInProgress)
        {
            background = AgendaPalette.GreenTint;
            stroke = AgendaPalette.GreenBorder;
            tag = "IN CHAIR";
            tagInk = AgendaPalette.OnGreen;
            tagFill = AgendaPalette.Green;
        }
        else if (booking.IsPending)
        {
            bar = AgendaPalette.Purple;
            stroke = AgendaPalette.PurpleBorder;
            dash = AgendaConstants.Dashed();
            tag = "PENDING";
            tagInk = AgendaPalette.Purple;
            tagFill = AgendaPalette.PurpleTint;
        }
        else if (booking.IsFinished || booking.LocalEnd <= now)
        {
            // Finished: the bar goes to the plain border colour and every line steps down one
            // token. No opacity on the row itself.
            bar = AgendaPalette.Line;
            titleInk = AgendaPalette.Dim;
            timeInk = AgendaPalette.Dim;
            subtitleInk = AgendaPalette.Dim;
            metaInk = AgendaPalette.Dim;

            if (booking.IsNoShow)
            {
                tag = "NO SHOW";
                tagInk = AgendaPalette.Muted;
                tagFill = AgendaPalette.SurfaceRaised;
            }
        }

        return new AgendaRow
        {
            Kind = AgendaRowKind.Booking,
            Start = booking.LocalStart,
            End = booking.LocalEnd,
            Booking = booking,
            TimeText = booking.TimeText,
            DurationText = booking.DurationText,
            Title = booking.CustomerName,
            Subtitle = string.Join(" · ", subtitleParts),
            BayText = bayText,
            ShowMarkCollected = showMarkCollected,
            TagText = tag,
            TagTextColor = tagInk,
            TagBackgroundColor = tagFill,
            BarColor = bar,
            RowBackgroundColor = background,
            RowStrokeColor = stroke,
            RowStrokeDash = dash,
            TitleColor = titleInk,
            TimeColor = timeInk,
            SubtitleColor = subtitleInk,
            MetaColor = metaInk,
        };
    }

    public static List<AgendaRow> BlockRows(
        AgendaBuildRequest request,
        List<(DateTimeOffset Start, DateTimeOffset End)> collectedRanges)
    {
        var rows = new List<AgendaRow>();

        var groups = request.Blocks
            .GroupBy(b => (b.StartsAt, b.EndsAt, Reason: b.Reason ?? string.Empty))
            .OrderBy(g => g.Key.StartsAt);

        foreach (var group in groups)
        {
            var start = LocalTime.ToLocal(group.Key.StartsAt);
            var end = LocalTime.ToLocal(group.Key.EndsAt);
            collectedRanges.Add((start, end));

            var affected = group
                .Select(b => request.OperatorNames.TryGetValue(b.OperatorId, out var name) ? name : null)
                .Where(name => name is not null)
                .Distinct()
                .ToList();

            var who = affected.Count >= request.ActiveOperatorCount && request.ActiveOperatorCount > 0
                ? request.ActiveOperatorCount == 2
                    ? $"Both {request.ResourcePluralNoun}"
                    : $"All {request.ResourcePluralNoun}"
                : string.Join(", ", affected!);

            var duration = AgendaBookingResponse.FormatDuration(end - start);

            rows.Add(new AgendaRow
            {
                Kind = AgendaRowKind.Blocked,
                Start = start,
                End = end,
                TimeText = start.ToString("HH:mm"),
                Title = group.Key.Reason.Length > 0 ? group.Key.Reason : "Blocked",
                Subtitle = who.Length > 0 ? $"{who} blocked · {duration}" : $"Blocked · {duration}",
                // Blocked time is not cancelled work, just unavailable, so it reads dim rather
                // than faded out.
                BarColor = AgendaPalette.Line,
                RowBackgroundColor = AgendaPalette.SurfaceRaised,
                RowStrokeColor = Colors.Transparent,
                TitleColor = AgendaPalette.Muted,
                TimeColor = AgendaPalette.Muted,
                SubtitleColor = AgendaPalette.Dim,
            });
        }

        return rows;
    }

    public static List<AgendaRow> GapRows(
        AgendaBuildRequest request,
        List<(DateTimeOffset Start, DateTimeOffset End)> occupied)
    {
        var rows = new List<AgendaRow>();

        var sellable = Merge(request.FreeSlots.Select(s =>
            (LocalTime.ToLocal(s.SlotStart), LocalTime.ToLocal(s.SlotEnd))));

        var idle = Subtract(sellable, Merge(occupied));
        var floor = TimeSpan.FromMinutes(Math.Max(1, request.ShortestServiceMinutes));

        foreach (var (start, end) in idle)
        {
            if (end - start < floor)
                continue;

            rows.Add(new AgendaRow
            {
                Kind = AgendaRowKind.Gap,
                Start = start,
                End = end,
                TimeText = start.ToString("HH:mm"),
                Title = $"{AgendaBookingResponse.FormatDuration(end - start)} free",
                Subtitle = "Nothing booked",
                BarColor = AgendaPalette.Line,
                RowBackgroundColor = Colors.Transparent,
                RowStrokeColor = AgendaPalette.Line,
                RowStrokeDash = AgendaConstants.Dashed(),
                TitleColor = AgendaPalette.Muted,
                TimeColor = AgendaPalette.Muted,
            });
        }

        return rows;
    }

    public static List<(DateTimeOffset Start, DateTimeOffset End)> Merge(
        IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> ranges)
    {
        var ordered = ranges.Where(r => r.End > r.Start).OrderBy(r => r.Start).ToList();
        var merged = new List<(DateTimeOffset Start, DateTimeOffset End)>();

        foreach (var range in ordered)
        {
            if (merged.Count > 0 && range.Start <= merged[^1].End)
            {
                if (range.End > merged[^1].End)
                    merged[^1] = (merged[^1].Start, range.End);

                continue;
            }

            merged.Add(range);
        }

        return merged;
    }

    public static List<(DateTimeOffset Start, DateTimeOffset End)> Subtract(
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> source,
        IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> cuts)
    {
        var result = new List<(DateTimeOffset Start, DateTimeOffset End)>();

        foreach (var range in source)
        {
            var remaining = new List<(DateTimeOffset Start, DateTimeOffset End)> { range };

            foreach (var cut in cuts)
            {
                var next = new List<(DateTimeOffset Start, DateTimeOffset End)>(remaining.Count + 1);

                foreach (var piece in remaining)
                {
                    if (cut.End <= piece.Start || cut.Start >= piece.End)
                    {
                        next.Add(piece);
                        continue;
                    }

                    if (cut.Start > piece.Start) next.Add((piece.Start, cut.Start));
                    if (cut.End < piece.End) next.Add((cut.End, piece.End));
                }

                remaining = next;
                if (remaining.Count == 0) break;
            }

            result.AddRange(remaining);
        }

        result.Sort((a, b) => a.Start.CompareTo(b.Start));
        return result;
    }
}
