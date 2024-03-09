namespace TestCommon.Constants;

/// <summary>
/// Provides the endpoint routes to test.
/// </summary>
public static class EndpointRoutes
{
    /// <summary>
    /// The routes of the attributes endpoint.
    /// </summary>
    public static class Attributes
    {
        /// <summary>
        /// The route of the get attributes endpoint.
        /// </summary>
        public const string GET_ATTRIBUTES = "/attributes";

        /// <summary>
        /// The route of the get leaf attributes endpoint.
        /// </summary>
        public const string GET_LEAF_ATTRIBUTES = "/attributes/leafAttributes";

        /// <summary>
        /// The route of the get sub attributes endpoint.
        /// </summary>
        public const string GET_SUB_ATTRIBUTES = "/attributes/subAttributes";

        /// <summary>
        /// The route of the update attribute values endpoint.
        /// </summary>
        public const string UPDATE_ATTRIBUTE_VALUES = "/attributes";
    }

    /// <summary>
    /// The routes of the categories endpoint.
    /// </summary>
    public static class Categories
    {
        /// <summary>
        /// The route of the get category mapping endpoint.
        /// </summary>
        public const string GET_CATEGORY_MAPPING = "/categories";

        /// <summary>
        /// The route of the get children or top level categories endpoint.
        /// </summary>
        public const string GET_CHILDREN_OR_TOP_LEVEL = "/categories/children";

        /// <summary>
        /// The route of the search categories endpoint.
        /// </summary>
        public const string SEARCH_CATEGORIES = "/categories/search";

        /// <summary>
        /// The route of the update category mapping endpoint.
        /// </summary>
        public const string UPDATE_CATEGORY_MAPPING = "/categories";
    }

    /// <summary>
    /// The routes of the root categories endpoint.
    /// </summary>
    public static class RootCategories
    {
        /// <summary>
        /// The route of the get root categories endpoint.
        /// </summary>
        public const string GET_ROOT_CATEGORIES = "/rootCategories";
    }
}
