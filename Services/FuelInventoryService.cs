using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GasStationMS.Data;
using GasStationMS.Models;

namespace GasStationMS.Services
{
    public interface IFuelInventoryService
    {
        Task<IEnumerable<FuelTank>> GetTanksByStationAsync(int stationId);
        Task<IEnumerable<FuelTank>> GetLowStockTanksAsync();
        Task<FuelDelivery> RegisterDeliveryAsync(FuelDelivery delivery);
        Task<decimal> GetTotalFuelVolumeAsync(int? stationId = null);

        /// <summary>
        /// FIFO-հիման վրա հանել ծավալ ռեզերվուարից և վերադարձնել weighted avg cost
        /// </summary>
        Task<decimal?> ConsumeFuelAsync(int tankId, decimal volumeLiters);
    }

    public class FuelInventoryService : IFuelInventoryService
    {
        private readonly ApplicationDbContext _db;
        public FuelInventoryService(ApplicationDbContext db) => _db = db;

        public async Task<IEnumerable<FuelTank>> GetTanksByStationAsync(int stationId) =>
            await _db.FuelTanks
                .Include(t => t.FuelType).Include(t => t.Station)
                .Include(t => t.Deliveries)
                .Where(t => t.StationId == stationId && t.IsActive)
                .OrderBy(t => t.TankCode).ToListAsync();

        public async Task<IEnumerable<FuelTank>> GetLowStockTanksAsync() =>
            await _db.FuelTanks
                .Include(t => t.FuelType).Include(t => t.Station)
                .Where(t => t.IsActive && t.CurrentVolumeLiters <= t.MinThresholdLiters)
                .ToListAsync();

        public async Task<FuelDelivery> RegisterDeliveryAsync(FuelDelivery delivery)
        {
            var tank = await _db.FuelTanks.FindAsync(delivery.FuelTankId)
                ?? throw new InvalidOperationException("Ռեզերվուարը չի գտնվել");

            if (tank.CurrentVolumeLiters + delivery.VolumeLiters > tank.CapacityLiters)
                throw new InvalidOperationException(
                    $"Ռեզերվուարի տարողությունը գերազանցում է։ " +
                    $"Առկա՝ {tank.CurrentVolumeLiters}լ, ավելացվում է՝ {delivery.VolumeLiters}լ, " +
                    $"տարողությունը՝ {tank.CapacityLiters}լ");

            delivery.TotalCost = delivery.VolumeLiters * delivery.PricePerLiter;
            delivery.RemainingLiters = delivery.VolumeLiters;   // new batch՝ full remaining
            delivery.DeliveredAt = DateTime.UtcNow;

            tank.CurrentVolumeLiters += delivery.VolumeLiters;
            tank.LastUpdated = DateTime.UtcNow;

            _db.FuelDeliveries.Add(delivery);
            await _db.SaveChangesAsync();
            return delivery;
        }

        public async Task<decimal> GetTotalFuelVolumeAsync(int? stationId = null)
        {
            var q = _db.FuelTanks.AsQueryable();
            if (stationId.HasValue) q = q.Where(t => t.StationId == stationId.Value);
            return await q.SumAsync(t => t.CurrentVolumeLiters);
        }

        /// <summary>
        /// FIFO algorithm:
        /// 1. Get all batches with RemainingLiters > 0, ordered by DeliveredAt ASC
        /// 2. Consume from oldest first
        /// 3. Return weighted average cost of consumed fuel
        /// </summary>
        public async Task<decimal?> ConsumeFuelAsync(int tankId, decimal volumeLiters)
        {
            var tank = await _db.FuelTanks.FindAsync(tankId);
            if (tank == null || tank.CurrentVolumeLiters < volumeLiters) return null;

            var batches = await _db.FuelDeliveries
                .Where(d => d.FuelTankId == tankId && d.RemainingLiters > 0)
                .OrderBy(d => d.DeliveredAt)
                .ToListAsync();

            decimal remaining = volumeLiters;
            decimal totalCost = 0m;

            foreach (var batch in batches)
            {
                if (remaining <= 0) break;
                var take = Math.Min(batch.RemainingLiters, remaining);
                totalCost += take * batch.PricePerLiter;
                batch.RemainingLiters -= take;
                remaining -= take;
            }

            // If still remaining, means not enough in batches (legacy data) —
            // fallback: take rest at average cost, no batch update
            if (remaining > 0)
            {
                // fallback cost calculation — use last known price or 0
                var lastPrice = batches.LastOrDefault()?.PricePerLiter ?? 0m;
                totalCost += remaining * lastPrice;
            }

            tank.CurrentVolumeLiters -= volumeLiters;
            tank.LastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return volumeLiters > 0 ? totalCost / volumeLiters : 0m;
        }
    }
}
