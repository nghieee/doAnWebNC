using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using web_ban_thuoc.Models;

namespace web_ban_thuoc.Services
{
    public class KnnProductRecommendation
    {
        public Product Product { get; set; } = null!;
        public double Distance { get; set; }
        public double SimilarityScore { get; set; }
        public int MatchPercentage { get; set; }
        public List<string> MatchingReasons { get; set; } = new();
    }

    public interface IRecommendationService
    {
        Task<List<Product>> GetRecommendationsAsync(int targetProductId, int limit = 4);
        Task<List<KnnProductRecommendation>> GetKnnRecommendationsAsync(int targetProductId, int k = 4);
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
            var knnList = await GetKnnRecommendationsAsync(targetProductId, limit);
            return knnList.Select(x => x.Product).ToList();
        }

        public async Task<List<KnnProductRecommendation>> GetKnnRecommendationsAsync(int targetProductId, int k = 4)
        {
            var targetProduct = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == targetProductId && p.IsActive);

            if (targetProduct == null)
                return new List<KnnProductRecommendation>();

            var candidateProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p => p.ProductId != targetProductId && p.IsActive)
                .ToListAsync();

            if (!candidateProducts.Any())
                return new List<KnnProductRecommendation>();

            var targetTokens = TokenizeText($"{targetProduct.Uses} {targetProduct.Ingredients} {targetProduct.TargetUsers}");

            // Tính toán Khoảng cách KNN (Distance Metric) giữa Target Product và từng Candidate Product
            var ratedCandidates = candidateProducts.Select(candidate =>
            {
                var matchingReasons = new List<string>();

                // Feature 1: Độ tương đồng danh mục ts4
                double categorySim = 0.0;
                if (targetProduct.CategoryId == candidate.CategoryId)
                {
                    categorySim = 1.0;
                    matchingReasons.Add("Cùng danh mục");
                }
                else if (targetProduct.Category?.ParentCategoryId != null &&
                         targetProduct.Category.ParentCategoryId == candidate.Category?.ParentCategoryId)
                {
                    categorySim = 0.5;
                    matchingReasons.Add("Danh mục tương đương");
                }

                // Feature 2: Độ tương đồng thương hiệu, nsx ts2
                double brandSim = 0.0;
                if (!string.IsNullOrEmpty(targetProduct.Brand) && !string.IsNullOrEmpty(candidate.Brand))
                {
                    if (targetProduct.Brand.Equals(candidate.Brand, StringComparison.OrdinalIgnoreCase))
                    {
                        brandSim = 1.0;
                        matchingReasons.Add($"Cùng hãng {candidate.Brand}");
                    }
                }

                // Feature 3: Độ tương đồng xuất xứ ts1
                double originSim = 0.0;
                if (!string.IsNullOrEmpty(targetProduct.Origin) && !string.IsNullOrEmpty(candidate.Origin))
                {
                    if (targetProduct.Origin.Equals(candidate.Origin, StringComparison.OrdinalIgnoreCase))
                    {
                        originSim = 1.0;
                        matchingReasons.Add($"Xuất xứ {candidate.Origin}");
                    }
                }

                // Feature 4: Độ tương đồng giá bán ts2
                double maxPrice = (double)Math.Max(targetProduct.Price, Math.Max(candidate.Price, 100000m));
                double priceDiff = (double)Math.Abs(targetProduct.Price - candidate.Price);
                double priceSim = Math.Max(0.0, 1.0 - (priceDiff / maxPrice));
                if (priceSim >= 0.8)
                {
                    matchingReasons.Add("Mức giá tương đồng");
                }

                // Feature 5: Độ tương đồng công dụng thành phần ts3
                var candidateTokens = TokenizeText($"{candidate.Uses} {candidate.Ingredients} {candidate.TargetUsers}");
                double textSim = CalculateJaccardSimilarity(targetTokens, candidateTokens);
                if (textSim >= 0.15)
                {
                    matchingReasons.Add("Công dụng & Thành phần tương đồng");
                }

                double maxWeight = 12.0;
                double totalScore = (categorySim * 4.0) + (brandSim * 2.0) + (originSim * 1.0) + (priceSim * 2.0) + (textSim * 3.0);

                double normalizedSimilarity = totalScore / maxWeight;
                double distance = 1.0 - normalizedSimilarity;
                int matchPercentage = (int)Math.Min(99, Math.Max(45, Math.Round(normalizedSimilarity * 100)));

                if (!matchingReasons.Any())
                {
                    matchingReasons.Add("Gợi ý phù hợp");
                }

                return new KnnProductRecommendation
                {
                    Product = candidate,
                    Distance = distance,
                    SimilarityScore = totalScore,
                    MatchPercentage = matchPercentage,
                    MatchingReasons = matchingReasons.Take(3).ToList()
                };
            })
            .OrderBy(x => x.Distance) // Thuật toán KNN: Lấy K láng giềng gần nhất (Khoảng cách Distance nhỏ nhất)
            .Take(k)
            .ToList();

            return ratedCandidates;
        }

        private HashSet<string> TokenizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new HashSet<string>();
            char[] delimiters = new[] { ' ', ',', '.', ';', ':', '-', '(', ')', '/', '\n', '\r', '\t' };
            var words = text.ToLowerInvariant()
                .Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToList();
            return new HashSet<string>(words);
        }

        private double CalculateJaccardSimilarity(HashSet<string> setA, HashSet<string> setB)
        {
            if (!setA.Any() || !setB.Any()) return 0.0;
            int intersection = setA.Count(x => setB.Contains(x));
            int union = setA.Union(setB).Count();
            if (union == 0) return 0.0;
            return (double)intersection / union;
        }
    }
}
