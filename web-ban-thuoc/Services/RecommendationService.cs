using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services
{
    public interface IRecommendationService
    {
        Task<List<Product>> GetRecommendationsAsync(int targetProductId, int limit = 4);
    }

    public class RecommendationService : IRecommendationService
    {
        private readonly LongChauDbContext _context;

        public RecommendationService(LongChauDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetRecommendationsAsync(int targetProductId, int limit = 4)
        {
            // Fetch the target product details
            var targetProduct = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == targetProductId && p.IsActive);

            if (targetProduct == null)
                return new List<Product>();

            // Fetch all other active products in the system (load essential fields for distance calculation)
            var candidateProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p => p.ProductId != targetProductId && p.IsActive)
                .ToListAsync();

            if (!candidateProducts.Any())
                return new List<Product>();

            var ratedCandidates = candidateProducts.Select(candidate =>
            {
                // Feature 1: Category Similarity (Weight: 5.0)
                double categorySim = (targetProduct.CategoryId == candidate.CategoryId) ? 1.0 : 0.0;

                // Feature 2: Brand/Manufacturer Similarity (Weight: 2.0)
                double brandSim = 0.0;
                if (!string.IsNullOrEmpty(targetProduct.Brand) && !string.IsNullOrEmpty(candidate.Brand))
                {
                    brandSim = targetProduct.Brand.Equals(candidate.Brand, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
                }

                // Feature 3: Origin Country Similarity (Weight: 1.0)
                double originSim = 0.0;
                if (!string.IsNullOrEmpty(targetProduct.Origin) && !string.IsNullOrEmpty(candidate.Origin))
                {
                    originSim = targetProduct.Origin.Equals(candidate.Origin, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
                }

                // Feature 4: Normalized Price Similarity (Weight: 2.0)
                double maxPrice = (double)Math.Max(targetProduct.Price, Math.Max(candidate.Price, 1m));
                double priceDiff = (double)Math.Abs(targetProduct.Price - candidate.Price);
                double priceSim = 1.0 - (priceDiff / maxPrice);

                // Calculate total similarity score out of 10.0
                double totalScore = (categorySim * 5.0) + (brandSim * 2.0) + (priceSim * 2.0) + (originSim * 1.0);
                double distance = 10.0 - totalScore; // KNN distance metrics (lower is closer/more similar)

                return new { Product = candidate, Distance = distance };
            })
            .OrderBy(x => x.Distance) // KNN retrieves nearest neighbors (smallest distance first)
            .Select(x => x.Product)
            .Take(limit)
            .ToList();

            return ratedCandidates;
        }
    }
}
