using DomainBase;

namespace DomainBase.Tests;

public class SpecificationTests
{
    private class Category { public string Title { get; set; } = string.Empty; }
    private class Product { public string Name { get; set; } = string.Empty; public decimal Price { get; set; } public Category? Category { get; set; } }

    private class NameContainsSpec : Specification<Product>
    {
        public NameContainsSpec(string term) : base(p => p.Name.Contains(term)) { }
        public NameContainsSpec WithInclude()
        {
            AddInclude(p => p.Category!);
            AddInclude("Category.Parent");
            return this;
        }
        public NameContainsSpec WithOrderBy() { ApplyOrderBy(p => p.Price); return this; }
        public NameContainsSpec WithOrderByDescending() { ApplyOrderByDescending(p => p.Price); return this; }
        public NameContainsSpec WithPaging(int skip, int take) { ApplyPaging(skip, take); return this; }
    }

    private class OrderSwitchSpec : Specification<Product>
    {
        public OrderSwitchSpec(bool descendingThenAscending) : base(p => true)
        {
            if (descendingThenAscending)
            {
                ApplyOrderByDescending(p => p.Price);
                ApplyOrderBy(p => p.Price);
            }
            else
            {
                ApplyOrderBy(p => p.Price);
                ApplyOrderByDescending(p => p.Price);
            }
        }
    }

    [Fact]
    public void Criteria_IsEvaluatedCorrectly()
    {
        var spec = new NameContainsSpec("ap");
        Assert.True(spec.IsSatisfiedBy(new Product { Name = "apple" }));
        Assert.False(spec.IsSatisfiedBy(new Product { Name = "pear" }));
    }

    [Fact]
    public void And_CombinesCriteriaAndMergesIncludes()
    {
        var left = new NameContainsSpec("a").WithInclude();
        var right = new NameContainsSpec("p").WithInclude();
        var combined = left.And(right);

        Assert.True(combined.IsSatisfiedBy(new Product { Name = "apple" }));
        Assert.False(combined.IsSatisfiedBy(new Product { Name = "banana" }));
        Assert.Equal(2, combined.IncludeStrings.Count); // one from each
        Assert.Equal(2, combined.Includes.Count); // one from each
    }

    [Fact]
    public void Or_CombinesCriteria()
    {
        var left = new NameContainsSpec("ki");
        var right = new NameContainsSpec("ap");
        var combined = left.Or(right);

        Assert.True(combined.IsSatisfiedBy(new Product { Name = "kiwi" }));
        Assert.True(combined.IsSatisfiedBy(new Product { Name = "apple" }));
        Assert.False(combined.IsSatisfiedBy(new Product { Name = "orange" }));
    }

    [Fact]
    public void Not_NegatesCriteria()
    {
        var spec = new NameContainsSpec("ap");
        var negated = spec.Not();
        Assert.False(negated.IsSatisfiedBy(new Product { Name = "apple" }));
        Assert.True(negated.IsSatisfiedBy(new Product { Name = "orange" }));
    }

    [Fact]
    public void OrderBy_And_OrderByDescending_AreMutuallyExclusive()
    {
        var asc = new NameContainsSpec("a").WithOrderBy();
        Assert.NotNull(asc.OrderBy);
        Assert.Null(asc.OrderByDescending);

        var desc = new NameContainsSpec("a").WithOrderByDescending();
        Assert.NotNull(desc.OrderByDescending);
        Assert.Null(desc.OrderBy);

        var lastWins1 = new OrderSwitchSpec(descendingThenAscending: true);
        Assert.NotNull(lastWins1.OrderBy);
        Assert.Null(lastWins1.OrderByDescending);

        var lastWins2 = new OrderSwitchSpec(descendingThenAscending: false);
        Assert.NotNull(lastWins2.OrderByDescending);
        Assert.Null(lastWins2.OrderBy);
    }

    [Fact]
    public void ApplyPaging_SetsSkipTakeAndEnablesPaging()
    {
        var spec = new NameContainsSpec("a").WithPaging(10, 5);
        Assert.True(spec.IsPagingEnabled);
        Assert.Equal(10, spec.Skip);
        Assert.Equal(5, spec.Take);
    }
}

