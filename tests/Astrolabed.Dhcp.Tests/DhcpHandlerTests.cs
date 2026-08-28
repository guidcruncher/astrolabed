// File: DhcpHandlerTests.cs
namespace Astrolabed.Dhcp.Tests;

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Data.Models;
using Astrolabed.Data.Pagination;
using Astrolabed.Data.Repositories;
using Astrolabed.Dhcp.Options;
using Astrolabed.Dhcp.Protocol;
using Astrolabed.Dhcp.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

public class DhcpHandlerTests
{
    private readonly TestLeaseRepository _repository = new();
    private readonly TestOptionsMonitor<DhcpServerOptions> _optionsMonitor = new();
    private readonly NullLogger<DhcpHandler> _logger = NullLogger<DhcpHandler>.Instance;

    [Fact]
    public async Task ProcessMessageAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = new DhcpHandler(_repository, _optionsMonitor, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.ProcessMessageAsync(null!));
    }

    [Fact]
    public async Task ProcessMessageAsync_MissingMessageTypeOption_ReturnsNull()
    {
        // Arrange
        var handler = new DhcpHandler(_repository, _optionsMonitor, _logger);
        var request = new DhcpMessage();

        // Act
        DhcpMessage? result = await handler.ProcessMessageAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ProcessMessageAsync_DiscoverMessage_AllocatesLeaseAndReturnsOffer()
    {
        // Arrange
        var handler = new DhcpHandler(_repository, _optionsMonitor, _logger);
        var request = new DhcpMessage
        {
            TransactionId = 12345,
            ClientHardwareAddress = [0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
        };
        request.Options.Add(DhcpOption.CreateByte(DhcpOptionCode.DhcpMessageType, (byte)DhcpMessageType.Discover));

        // Act
        DhcpMessage? response = await handler.ProcessMessageAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(DhcpOpCode.BootReply, response.Operation);
        Assert.Equal(DhcpMessageType.Offer, response.GetMessageType());
        Assert.Equal(12345u, response.TransactionId);
    }

    [Fact]
    public async Task ProcessMessageAsync_ReleaseMessage_ReleasesLeaseAndReturnsNull()
    {
        // Arrange
        var handler = new DhcpHandler(_repository, _optionsMonitor, _logger);
        var request = new DhcpMessage
        {
            ClientHardwareAddress = [0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
        };
        request.Options.Add(DhcpOption.CreateByte(DhcpOptionCode.DhcpMessageType, (byte)DhcpMessageType.Release));

        // Act
        DhcpMessage? response = await handler.ProcessMessageAsync(request);

        // Assert
        Assert.Null(response);
        Assert.True(_repository.ReleaseCalled);
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T> where T : class, new()
    {
        public T CurrentValue { get; set; } = new();

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class TestLeaseRepository : IDhcpLeaseRepository
    {
        public bool ReleaseCalled { get; private set; }

        public Task<DhcpLease?> GetLeaseByClientIdOrMacAsync(string clientId, string macAddress, CancellationToken cancellationToken = default) =>
            Task.FromResult<DhcpLease?>(null);

        public Task<DhcpLease?> GetLeaseByPtrAddressAsync(string ptrAddress, CancellationToken cancellationToken = default) =>
            Task.FromResult<DhcpLease?>(null);

        public Task<PagedResult<DhcpLease>> GetLeasesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<PagedResult<DhcpLease>>(null!);

        public Task<DhcpLease?> GetLeaseByIpAsync(IPAddress ipAddress, CancellationToken cancellationToken = default) =>
            Task.FromResult<DhcpLease?>(null);

        public Task<bool> IsIpAvailableAsync(IPAddress ipAddress, string clientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<DhcpLease> AllocateOrUpdateLeaseAsync(
            string clientId, string clientName, string macAddress, IPAddress requestedIp, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DhcpLease
            {
                ClientId = clientId,
                ClientName = clientName,
                MacAddress = macAddress,
                IpAddress = requestedIp
            });
        }

        public Task ReleaseLeaseAsync(string clientId, string macAddress, CancellationToken cancellationToken = default)
        {
            ReleaseCalled = true;
            return Task.CompletedTask;
        }
    }
}

