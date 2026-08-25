using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Business.Abstract;
using RestaurantMenu.Business.Common;
using RestaurantMenu.Business.Orders;
using RestaurantMenu.DataAccess.Context;
using RestaurantMenu.Entities.Enums;
using RestaurantMenu.Entities.Models;

namespace RestaurantMenu.Business.Concrete;

public class ServiceRequestManager : IServiceRequestService
{
    private readonly AppDbContext _db;

    public ServiceRequestManager(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<ServiceRequest>> CreateAsync(
        int tableId,
        ServiceRequestType type,
        CancellationToken cancellationToken = default)
    {
        var table = await _db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == tableId && t.IsActive, cancellationToken);
        if (table is null)
        {
            return ServiceResult<ServiceRequest>.Fail("Masa bulunamadı.");
        }

        var hasOpen = await _db.ServiceRequests.AnyAsync(
            r => r.TableId == tableId && r.Type == type && r.Status != ServiceRequestStatus.Completed,
            cancellationToken);

        if (hasOpen)
        {
            return ServiceResult<ServiceRequest>.Fail("Bu masa için zaten açık bir talep var.");
        }

        var request = new ServiceRequest
        {
            TableId = tableId,
            Type = type,
            Status = ServiceRequestStatus.Pending
        };

        _db.ServiceRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult<ServiceRequest>.Ok(request);
    }

    public async Task<IReadOnlyList<ServiceRequest>> GetOpenAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        return await _db.ServiceRequests
            .Include(r => r.Table)
            .Where(r => r.Table.RestaurantId == restaurantId && r.Status != ServiceRequestStatus.Completed)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult> ChangeStatusAsync(
        int id,
        ServiceRequestStatus status,
        string userId,
        int restaurantId,
        CancellationToken cancellationToken = default)
    {
        var request = await _db.ServiceRequests
            .Include(r => r.Table)
            .FirstOrDefaultAsync(r => r.Id == id && r.Table.RestaurantId == restaurantId, cancellationToken);
        if (request is null)
        {
            return ServiceResult.Fail("Talep bulunamadı.");
        }

        if (!ServiceRequestMachine.CanTransition(request.Status, status))
        {
            return ServiceResult.Fail("Bu talep durumu geçişine izin verilmiyor.");
        }

        request.Status = status;
        request.HandledByUserId = userId;
        request.HandledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }
}
