using Booking.Api.Domain;
using FluentAssertions;

namespace Booking.Api.Tests;

public class BookingRulesTests
{
    [Fact]
    public void Overlapper_returnerer_true_nar_intervallene_overlapper()
    {
        var startA = new DateTimeOffset(2026, 01, 28, 10, 0, 0, TimeSpan.Zero);
        var sluttA = new DateTimeOffset(2026, 01, 28, 11, 0, 0, TimeSpan.Zero);

        var startB = new DateTimeOffset(2026, 01, 28, 10, 30, 0, TimeSpan.Zero);
        var sluttB = new DateTimeOffset(2026, 01, 28, 11, 30, 0, TimeSpan.Zero);

        BookingRules.Overlapper(startA, sluttA, startB, sluttB).Should().BeTrue();
    }

    [Fact]
    public void Overlapper_returnerer_false_nar_intervallene_ikke_overlapper()
    {
        var startA = new DateTimeOffset(2026, 01, 28, 10, 0, 0, TimeSpan.Zero);
        var sluttA = new DateTimeOffset(2026, 01, 28, 11, 0, 0, TimeSpan.Zero);

        var startB = new DateTimeOffset(2026, 01, 28, 11, 0, 0, TimeSpan.Zero);
        var sluttB = new DateTimeOffset(2026, 01, 28, 12, 0, 0, TimeSpan.Zero);

        // SluttA == StartB skal IKKE regnes som overlapp (back-to-back er lov)
        BookingRules.Overlapper(startA, sluttA, startB, sluttB).Should().BeFalse();
    }
}
