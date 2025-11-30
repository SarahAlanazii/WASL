using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wasl.Data;
using Wasl.Infrastructure;
using Wasl.ViewModels.ShipmentVMs;

namespace Wasl.Controllers
{
    /// <summary>
    /// Front Controller - Handles public pages (landing, shipments browsing, shipment details)
    /// </summary>
    public class FrontController : BaseController
    {
        public FrontController(
            WaslDbContext context,
            ILogger<FrontController> logger,
            IFileUploadService fileUpload)
            : base(context, logger, fileUpload)
        {
        }

        /// <summary>
        /// Landing page (home page)
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Shipments(ShipmentFilterViewModel filter)
        {
            var query = _context.ShipmentRequests
                .Include(s => s.Company)
                .Include(s => s.Bids)
                .Where(s => s.Status == AppConstants.SHIPMENT_PENDING ||
                           s.Status == AppConstants.SHIPMENT_BIDDING)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.Search))
            {
                query = query.Where(s =>
                    s.GoodsType.Contains(filter.Search) ||
                    s.PickupCity.Contains(filter.Search) ||
                    s.DeliveryCity.Contains(filter.Search) ||
                    s.PickupRegion.Contains(filter.Search) ||
                    s.DeliveryRegion.Contains(filter.Search));
            }

            if (!string.IsNullOrEmpty(filter.GoodsType))
                query = query.Where(s => s.GoodsType == filter.GoodsType);

            if (!string.IsNullOrEmpty(filter.Region))
                query = query.Where(s => s.PickupRegion == filter.Region);

            if (!string.IsNullOrEmpty(filter.City))
                query = query.Where(s => s.PickupCity == filter.City);

            if (!string.IsNullOrEmpty(filter.WeightRange))
            {
                query = filter.WeightRange switch
                {
                    "0-100" => query.Where(s => s.WeightKg <= 100),
                    "100-500" => query.Where(s => s.WeightKg > 100 && s.WeightKg <= 500),
                    "500-1000" => query.Where(s => s.WeightKg > 500 && s.WeightKg <= 1000),
                    "1000+" => query.Where(s => s.WeightKg > 1000),
                    _ => query
                };
            }

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(s => s.Status == filter.Status);

            // Apply sorting
            query = (filter.Sort ?? "newest") switch
            {
                "deadline" => query.OrderBy(s => s.DeliveryDeadline),
                "weight" => query.OrderByDescending(s => s.WeightKg),
                "newest" => query.OrderByDescending(s => s.RequestDate),
                _ => query.OrderByDescending(s => s.RequestDate)
            };

            // Pagination
            int pageSize = 12;
            int pageNumber = filter.Page ?? 1;
            var totalItems = await query.CountAsync();

            var shipments = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get bid counts
            var shipmentIds = shipments.Select(s => s.ShipmentRequestId).ToList();
            var bidsCounts = await _context.Bids
                .Where(b => shipmentIds.Contains(b.ShipmentRequestId ?? 0))
                .GroupBy(b => b.ShipmentRequestId)
                .Select(g => new { ShipmentRequestId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ShipmentRequestId ?? 0, x => x.Count);

            // Get unique goods types for filter
            var goodsTypes = await _context.ShipmentRequests
                .Where(s => s.Status == AppConstants.SHIPMENT_PENDING ||
                           s.Status == AppConstants.SHIPMENT_BIDDING)
                .Select(s => s.GoodsType)
                .Distinct()
                .ToListAsync();

            var viewModel = new ShipmentsListViewModel
            {
                Shipments = shipments,
                BidsCounts = bidsCounts,
                GoodsTypes = goodsTypes,
                Regions = KSALocations.Regions,
                Filter = filter,
                TotalItems = totalItems,
                PageSize = pageSize,
                CurrentPage = pageNumber
            };

            return View(viewModel);
        }

        /// <summary>
        /// View shipment details
        /// GET: /Front/ShipmentDetails/{id}
        /// </summary>
        public async Task<IActionResult> ShipmentDetails(int id)
        {
            var shipment = await _context.ShipmentRequests
                .Include(s => s.Company)
                .FirstOrDefaultAsync(s => s.ShipmentRequestId == id);

            if (shipment == null)
                return NotFound();

            var bids = await _context.Bids
                .Include(b => b.Provider)
                .Where(b => b.ShipmentRequestId == id)
                .OrderBy(b => b.BidPrice)
                .ToListAsync();

            var bidStats = new BidStatistics
            {
                AverageBid = bids.Any() ? bids.Average(b => b.BidPrice ?? 0) : 0,
                MinBid = bids.Any() ? bids.Min(b => b.BidPrice ?? 0) : 0,
                MaxBid = bids.Any() ? bids.Max(b => b.BidPrice ?? 0) : 0,
                ActiveBidders = bids.Select(b => b.ProviderId).Distinct().Count()
            };

            var viewModel = new ShipmentDetailsViewModel
            {
                Shipment = shipment,
                Bids = bids,
                BidStats = bidStats
            };

            return View(viewModel);
        }

        /// <summary>
        /// Privacy policy page
        /// GET: /Front/Privacy
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Error page - handles application errors
        /// GET: /Front/Error
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}