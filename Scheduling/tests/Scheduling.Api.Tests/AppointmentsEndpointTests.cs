using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Scheduling.Tests;

/// <summary>End-to-end tests over the real HTTP pipeline: routing, binding, handlers, domain.</summary>
public sealed class AppointmentsEndpointTests : IDisposable
{
    private readonly SchedulingApiFactory _factory = new();
    private readonly HttpClient _client;

    /// <summary>Starts an API with its own stores.</summary>
    public AppointmentsEndpointTests() => _client = _factory.CreateClient();

    /// <inheritdoc/>
    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Books_an_appointment()
    {
        DateTimeOffset start = SchedulingApiFactory.NextMondayAt(10);

        HttpResponseMessage response = await Book(start, "Priya Raman");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        JsonElement booked = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Priya Raman", booked.GetProperty("patientName").GetString());
        Assert.Equal(start, booked.GetProperty("startsAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Rejects_an_overlapping_appointment()
    {
        DateTimeOffset start = SchedulingApiFactory.NextMondayAt(11);
        Assert.Equal(HttpStatusCode.Created, (await Book(start, "First Patient")).StatusCode);

        HttpResponseMessage clash = await Book(start.AddMinutes(15), "Second Patient");

        Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
        JsonElement problem = await clash.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Scheduling rule violated", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Reschedules_an_appointment()
    {
        DateTimeOffset start = SchedulingApiFactory.NextMondayAt(12);
        JsonElement booked = await (await Book(start, "Moved Patient")).Content.ReadFromJsonAsync<JsonElement>();
        Guid id = booked.GetProperty("id").GetGuid();

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/appointments/{id}/slot",
            new { startsAt = start.AddHours(2) });

        response.EnsureSuccessStatusCode();
        JsonElement moved = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(start.AddHours(2), moved.GetProperty("startsAt").GetDateTimeOffset());
        Assert.Equal(start.AddHours(2).AddMinutes(30), moved.GetProperty("endsAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Cancels_an_appointment()
    {
        DateTimeOffset start = SchedulingApiFactory.NextMondayAt(13);
        JsonElement booked = await (await Book(start, "Cancelling Patient")).Content.ReadFromJsonAsync<JsonElement>();
        Guid id = booked.GetProperty("id").GetGuid();

        HttpResponseMessage response = await _client.DeleteAsync($"/api/appointments/{id}?reason=flu");

        response.EnsureSuccessStatusCode();
        JsonElement cancelled = await response.Content.ReadFromJsonAsync<JsonElement>();

        // AppointmentStatus.Cancelled == 2.
        Assert.Equal(2, cancelled.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Lists_the_appointments_of_one_day()
    {
        DateTimeOffset start = SchedulingApiFactory.NextMondayAt(14);
        Assert.Equal(HttpStatusCode.Created, (await Book(start, "Listed Patient")).StatusCode);

        JsonElement day = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/appointments?day={start:yyyy-MM-dd}");

        Assert.Equal(1, day.GetArrayLength());
        Assert.Equal("Listed Patient", day[0].GetProperty("patientName").GetString());
    }

    [Fact]
    public async Task Finds_free_slots_and_drops_the_booked_one()
    {
        DateTimeOffset start = SchedulingApiFactory.NextMondayAt(15);
        string day = start.ToString("yyyy-MM-dd");

        JsonElement before = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/availability/{SchedulingApiFactory.AdaId}/slots?day={day}&durationMinutes=30");
        Assert.Equal(16, before.GetArrayLength());   // 09:00-17:00 in half hours

        Assert.Equal(HttpStatusCode.Created, (await Book(start, "Slot Taker")).StatusCode);

        JsonElement after = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/availability/{SchedulingApiFactory.AdaId}/slots?day={day}&durationMinutes=30");
        Assert.Equal(15, after.GetArrayLength());
        Assert.DoesNotContain(
            after.EnumerateArray(),
            slot => slot.GetProperty("startsAt").GetDateTimeOffset() == start);
    }

    [Fact]
    public async Task Lists_the_seeded_providers()
    {
        JsonElement providers = await _client.GetFromJsonAsync<JsonElement>("/api/providers");

        Assert.Equal(2, providers.GetArrayLength());
        Assert.Equal("Ada Whitfield", providers[0].GetProperty("displayName").GetString());
    }

    private Task<HttpResponseMessage> Book(DateTimeOffset start, string patient) =>
        _client.PostAsJsonAsync("/api/appointments", new
        {
            providerId = SchedulingApiFactory.AdaId,
            patientName = patient,
            startsAt = start,
            durationMinutes = 30,
        });
}
