namespace YggdrasilApi.Models;

public class Realm
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<Template> Templates { get; set; } = new List<Template>();
    public List<Tag> Tags { get; set; } = new List<Tag>();
    //Root tags
    public List<TagTreeNode> TagTree { get; set; } = new List<TagTreeNode>();
    public class TagTreeNode
    {
        public int TagId { get; set; }
        public List<TagTreeNode> Parents { get; set; } = new List<TagTreeNode>();
        public List<TagTreeNode> Children { get; set; } = new List<TagTreeNode>();
        public TagTreeNode(Tag tag)
        {
            TagId = tag.Id;
        }
        public List<TagTreeNode> GetAllAncestors()
        {
            var ancestors = new List<TagTreeNode>();
            foreach (var parent in Parents)
            {
                ancestors.Add(parent);
                ancestors.AddRange(parent.GetAllAncestors());
            }
            return ancestors.Distinct().ToList();
        }
        public void AddParent(TagTreeNode parent)
        {
            if (!Parents.Contains(parent) && // No duplicate parents
                !parent.GetAllAncestors().Contains(this)) // Prevent circular reference
            {
                Parents.Add(parent);
                parent.Children.Add(this);
            }
        }
         public void AddChild(TagTreeNode child)
        {
            if (!Children.Contains(child))
            {
                Children.Add(child);
                child.Parents.Add(this);
            }
        }
    }
}
