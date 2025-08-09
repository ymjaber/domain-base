using DomainBase;

namespace DomainBase.Tests;

public class DomainExceptionsTests
{
    [Fact]
    public void EntityNotFoundException_SetsPropertiesAndErrorCode()
    {
        var ex = new EntityNotFoundException("Order", 42);
        Assert.Equal("Order", ex.EntityName);
        Assert.Equal(42, ex.Id);
        Assert.Equal("EntityNotFound", ex.ErrorCode);
        Assert.Contains("Order", ex.Message);
        Assert.Contains("42", ex.Message);
    }

    [Fact]
    public void BusinessRuleViolationException_SetsRuleNameAndErrorCode()
    {
        var ex = new BusinessRuleViolationException("MustBeActive", "Not active");
        Assert.Equal("MustBeActive", ex.RuleName);
        Assert.Equal("BusinessRule.MustBeActive", ex.ErrorCode);
        Assert.Contains("Not active", ex.Message);
    }

    [Fact]
    public void DomainValidationException_WithDictionary_SetsErrorsAndErrorCode()
    {
        var errors = new Dictionary<string, string[]> { ["Name"] = new[] { "Required" } };
        var ex = new DomainValidationException(errors);
        Assert.Equal("ValidationFailed", ex.ErrorCode);
        Assert.True(ex.Errors.ContainsKey("Name"));
        Assert.Contains("Required", ex.Errors["Name"]);
    }

    [Fact]
    public void DomainValidationException_WithPropertyAndMessage_PopulatesErrors()
    {
        var ex = new DomainValidationException("Email", "Invalid");
        Assert.True(ex.Errors.ContainsKey("Email"));
        Assert.Contains("Invalid", ex.Errors["Email"]);
    }

    [Fact]
    public void InvariantViolationException_SetsInvariantNameAndErrorCode()
    {
        var ex = new InvariantViolationException("StockNonNegative", "Negative stock");
        Assert.Equal("StockNonNegative", ex.InvariantName);
        Assert.Equal("Invariant.StockNonNegative", ex.ErrorCode);
        Assert.Contains("Negative stock", ex.Message);
    }

    [Fact]
    public void InvalidOperationDomainException_DefaultAndParameterized_ConstructorsWork()
    {
        var ex1 = new InvalidOperationDomainException("Generic error");
        Assert.Equal("InvalidOperation", ex1.ErrorCode);
        Assert.Null(ex1.Operation);
        Assert.Null(ex1.Reason);

        var ex2 = new InvalidOperationDomainException("CloseOrder", "Already shipped");
        Assert.Equal("InvalidOperation", ex2.ErrorCode);
        Assert.Equal("CloseOrder", ex2.Operation);
        Assert.Equal("Already shipped", ex2.Reason);
        Assert.Contains("CloseOrder", ex2.Message);
        Assert.Contains("Already shipped", ex2.Message);
    }
}

