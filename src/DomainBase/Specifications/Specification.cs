using System.Linq.Expressions;

namespace DomainBase;

/// <summary>
/// Base implementation of the specification pattern.
/// </summary>
/// <typeparam name="T">The type of entity to evaluate.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Specification{T}"/> class.
    /// </summary>
    /// <param name="criteria">The expression that defines the criteria.</param>
    protected Specification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Specification{T}"/> class with no criteria.
    /// </summary>
    protected Specification()
    {
        Criteria = _ => true;
    }

    /// <inheritdoc/>
    public Expression<Func<T, bool>> Criteria { get; }

    /// <inheritdoc/>
    public List<Expression<Func<T, object>>> Includes { get; } = new();

    /// <inheritdoc/>
    public List<string> IncludeStrings { get; } = new();

    /// <inheritdoc/>
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <inheritdoc/>
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <inheritdoc/>
    public int? Take { get; private set; }

    /// <inheritdoc/>
    public int? Skip { get; private set; }

    /// <inheritdoc/>
    public bool IsPagingEnabled { get; private set; }

    /// <inheritdoc/>
    public virtual bool IsSatisfiedBy(T entity)
    {
        var predicate = Criteria.Compile();
        return predicate(entity);
    }

    /// <summary>
    /// Adds an include expression for eager loading.
    /// </summary>
    /// <param name="includeExpression">The include expression.</param>
    /// <returns>The current specification instance.</returns>
    protected Specification<T> AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
        return this;
    }

    /// <summary>
    /// Adds an include string for eager loading.
    /// </summary>
    /// <param name="includeString">The include string path.</param>
    /// <returns>The current specification instance.</returns>
    protected Specification<T> AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
        return this;
    }

    /// <summary>
    /// Applies ordering to the specification.
    /// </summary>
    /// <param name="orderByExpression">The order by expression.</param>
    /// <returns>The current specification instance.</returns>
    protected Specification<T> ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
        OrderByDescending = null;
        return this;
    }

    /// <summary>
    /// Applies descending ordering to the specification.
    /// </summary>
    /// <param name="orderByDescendingExpression">The order by descending expression.</param>
    /// <returns>The current specification instance.</returns>
    protected Specification<T> ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
        OrderBy = null;
        return this;
    }

    /// <summary>
    /// Applies paging to the specification.
    /// </summary>
    /// <param name="skip">The number of items to skip.</param>
    /// <param name="take">The number of items to take.</param>
    /// <returns>The current specification instance.</returns>
    protected Specification<T> ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
        return this;
    }

    /// <summary>
    /// Combines this specification with another using AND logic.
    /// </summary>
    /// <param name="specification">The specification to combine with.</param>
    /// <returns>A new combined specification.</returns>
    public Specification<T> And(ISpecification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    /// <summary>
    /// Combines this specification with another using OR logic.
    /// </summary>
    /// <param name="specification">The specification to combine with.</param>
    /// <returns>A new combined specification.</returns>
    public Specification<T> Or(ISpecification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }

    /// <summary>
    /// Creates a specification that is the negation of this specification.
    /// </summary>
    /// <returns>A new negated specification.</returns>
    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}

/// <summary>
/// Represents a specification that combines two specifications with AND logic.
/// </summary>
internal class AndSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    /// <summary>
    /// Creates a new specification that is satisfied only when both <paramref name="left"/> and <paramref name="right"/> are satisfied.
    /// </summary>
    /// <param name="left">The left-hand specification.</param>
    /// <param name="right">The right-hand specification.</param>
    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
        : base(left.Criteria.And(right.Criteria))
    {
        _left = left;
        _right = right;
        
        Includes.AddRange(left.Includes);
        Includes.AddRange(right.Includes);
        IncludeStrings.AddRange(left.IncludeStrings);
        IncludeStrings.AddRange(right.IncludeStrings);
    }

    /// <inheritdoc />
    public override bool IsSatisfiedBy(T entity)
    {
        return _left.IsSatisfiedBy(entity) && _right.IsSatisfiedBy(entity);
    }
}

/// <summary>
/// Represents a specification that combines two specifications with OR logic.
/// </summary>
internal class OrSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    /// <summary>
    /// Creates a new specification that is satisfied when either <paramref name="left"/> or <paramref name="right"/> is satisfied.
    /// </summary>
    /// <param name="left">The left-hand specification.</param>
    /// <param name="right">The right-hand specification.</param>
    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
        : base(left.Criteria.Or(right.Criteria))
    {
        _left = left;
        _right = right;
        
        Includes.AddRange(left.Includes);
        Includes.AddRange(right.Includes);
        IncludeStrings.AddRange(left.IncludeStrings);
        IncludeStrings.AddRange(right.IncludeStrings);
    }

    /// <inheritdoc />
    public override bool IsSatisfiedBy(T entity)
    {
        return _left.IsSatisfiedBy(entity) || _right.IsSatisfiedBy(entity);
    }
}

/// <summary>
/// Represents a specification that negates another specification.
/// </summary>
internal class NotSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _specification;

    /// <summary>
    /// Creates a new specification that is satisfied when <paramref name="specification"/> is not satisfied.
    /// </summary>
    /// <param name="specification">The specification to negate.</param>
    public NotSpecification(ISpecification<T> specification)
        : base(specification.Criteria.Not())
    {
        _specification = specification;
        
        Includes.AddRange(specification.Includes);
        IncludeStrings.AddRange(specification.IncludeStrings);
    }

    /// <inheritdoc />
    public override bool IsSatisfiedBy(T entity)
    {
        return !_specification.IsSatisfiedBy(entity);
    }
}

/// <summary>
/// Extension methods for Expression manipulation in specifications.
/// </summary>
internal static class ExpressionExtensions
{
    /// <summary>
    /// Combines two predicate expressions using logical AND, producing a single expression with a shared parameter.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="left">The left-hand predicate.</param>
    /// <param name="right">The right-hand predicate.</param>
    /// <returns>An expression representing <c>left AND right</c>.</returns>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftVisitor = new ReplaceExpressionVisitor(left.Parameters[0], parameter);
        var leftBody = leftVisitor.Visit(left.Body) ?? throw new InvalidOperationException("Left expression body cannot be null");
        var rightVisitor = new ReplaceExpressionVisitor(right.Parameters[0], parameter);
        var rightBody = rightVisitor.Visit(right.Body) ?? throw new InvalidOperationException("Right expression body cannot be null");
        
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    /// <summary>
    /// Combines two predicate expressions using logical OR, producing a single expression with a shared parameter.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="left">The left-hand predicate.</param>
    /// <param name="right">The right-hand predicate.</param>
    /// <returns>An expression representing <c>left OR right</c>.</returns>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftVisitor = new ReplaceExpressionVisitor(left.Parameters[0], parameter);
        var leftBody = leftVisitor.Visit(left.Body) ?? throw new InvalidOperationException("Left expression body cannot be null");
        var rightVisitor = new ReplaceExpressionVisitor(right.Parameters[0], parameter);
        var rightBody = rightVisitor.Visit(right.Body) ?? throw new InvalidOperationException("Right expression body cannot be null");
        
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(leftBody, rightBody), parameter);
    }

    /// <summary>
    /// Negates a predicate expression, producing a single expression with a new parameter.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="expression">The predicate to negate.</param>
    /// <returns>An expression representing the logical NOT of <paramref name="expression"/>.</returns>
    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
    {
        var parameter = Expression.Parameter(typeof(T));
        var visitor = new ReplaceExpressionVisitor(expression.Parameters[0], parameter);
        var body = visitor.Visit(expression.Body) ?? throw new InvalidOperationException("Expression body cannot be null");
        
        return Expression.Lambda<Func<T, bool>>(Expression.Not(body), parameter);
    }

    private class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        /// <summary>
        /// Initializes a new instance of the visitor that replaces all occurrences of <paramref name="oldValue"/> with <paramref name="newValue"/>.
        /// </summary>
        /// <param name="oldValue">The expression to replace.</param>
        /// <param name="newValue">The replacement expression.</param>
        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        /// <inheritdoc />
        public override Expression? Visit(Expression? node)
        {
            if (node == _oldValue)
                return _newValue;
            return base.Visit(node);
        }
    }
}