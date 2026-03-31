using Microsoft.EntityFrameworkCore;
using Serilog;
using StockFlow.Web.Data;
using StockFlow.Web.Exceptions;
using StockFlow.Web.Services.Interfaces;

namespace StockFlow.Web.Services
{
    public class WeightValidatorService : IWeightValidatorService
    {
        private readonly AppDbContext _db;

        public WeightValidatorService(AppDbContext db)
        {
            _db = db;
        }

        public async Task ValidateChildWeightsAsync(int parentId, IEnumerable<double> childWeights, CancellationToken ct = default)
        {
            try
            {
                var parent = await _db.ProcessedItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProcessedItemId == parentId, ct)
                    ?? throw new NotFoundException(ErrorMessages.Process.ParentNotFound);

                var existingChildWeight = await _db.ProcessedItems
                    .AsNoTracking()
                    .Where(p => p.ParentId == parentId)
                    .SumAsync(p => p.OutputWeight, ct);

                var newChildTotal = childWeights.Sum();
                var totalAfter = existingChildWeight + newChildTotal;

                if (totalAfter > parent.OutputWeight)
                    throw new WeightValidationException(parent.OutputWeight, totalAfter);
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                Log.Error(ex, "Error validating child weights for parent {ParentId}", parentId);
                throw new AppException(ErrorMessages.General.ServerError);
            }
        }

        public Task ValidateSingleWeightAsync(double weight, string fieldName, CancellationToken ct = default)
        {
            if (weight <= 0)
                throw new ValidationException($"{fieldName} must be greater than zero.");

            return Task.CompletedTask;
        }

        public double GetRemainingWeight(double parentWeight, IEnumerable<double> existingChildWeights)
            => parentWeight - existingChildWeights.Sum();
    }
}