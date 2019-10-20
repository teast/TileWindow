using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TileWindow.Nodes;

namespace TileWindow.Extra.GraphViz
{
    public class NodeParseResult
    {
        public GraphBase Element { get; set; }
        public List<GraphObject> Objects { get; set; }
        public List<GraphRelation> Relations { get; set; }
        public List<GraphCluster> Clusters { get; set; }

        public NodeParseResult()
        {

        }

        public NodeParseResult(GraphBase element, List<GraphObject> objects, List<GraphRelation> relations, List<GraphCluster> clusters)
        {
            Element = element;
            Objects = objects;
            Relations = relations;
            Clusters = clusters;
        }
    }

    public class NodeTraveler
    {
        public IList<GraphObject> Objects { get; set; }
        public IList<GraphCluster> Clusters { get; set; }
        public IList<GraphRelation> Relations { get; set; }

        private int _global_object_index;
        private int _global_cluster_index;

        public void Travel(IVirtualDesktop start)
        {
            _global_object_index = 0;
            _global_cluster_index = 1;

            var result = ParseNode(start);
            Objects = result.Objects;
            Clusters = result.Clusters;
            Relations = result.Relations;

            if (result.Element is GraphCluster)
            {
                var cc = result.Element as GraphCluster;
                Clusters.Add(cc);
            }
            else
            {
                Objects.Add(result.Element as GraphObject);
            }
        }

        protected virtual NodeParseResult ParseNode(IVirtualDesktop node)
        {
            var result = ParseContainer("VirtualDesktop #" + (node.Index+1) + " (id: " + node.Index + ")", node.Direction, node.MyFocusNode, node.FocusNode, node.Childs);
            if (node.FloatingNodes.Count == 0)
            {
                return result;
            }

            var desktop = result.Element as GraphCluster;
            var relations = new List<GraphRelation>();
            var clusters  = new List<GraphCluster>();
            var objects = new List<GraphObject>();

            // Create a new frame for all floating objects and add an relation to small circle above
            var floaters = AllocCluster();
            floaters.Label = "Floating nodes";
            ParseChilds(ref floaters, ref objects, ref relations, ref clusters, node.MyFocusNode, node.FocusNode, node.FloatingNodes);

            // Alloc small circle inside desktop frame
            var obj = AllocObject();
            obj.Label = "Floating";
            obj.Shape = GraphShapeEnum.Circle;
            desktop.Objects.Add(obj);

            relations.Add(new GraphRelation(obj, floaters.RelateToCluster, relateFromObj2Cluster: floaters));
            clusters.Add(floaters);
            relations.AddRange(result.Relations);
            clusters.AddRange(result.Clusters);
            return new NodeParseResult(result.Element, result.Objects, relations, clusters);
        }

        protected virtual NodeParseResult ParseNode(Node node)
        {
            var t = node.GetType();

            if (t == typeof(FixedContainerNode))
            {
                var myFocus = (node as FixedContainerNode).Desktop.FocusTracker.MyLastFocusNode(node);
                var focus = (node as FixedContainerNode).Desktop.FocusTracker.FocusNode();
                return ParseContainer("FixedContainer", node.Direction, myFocus, focus, (node as FixedContainerNode).Childs);
            }
            else if (t == typeof(ScreenNode))
            {
                var myFocus = (node as ScreenNode).Desktop.FocusTracker.MyLastFocusNode(node);
                var focus = (node as ScreenNode).Desktop.FocusTracker.FocusNode();
                return ParseContainer("ScreenContainer", node.Direction, myFocus, focus, (node as ScreenNode).Childs);
            }
            else if (t == typeof(ContainerNode))
            {
                var myFocus = (node as ContainerNode).Desktop.FocusTracker.MyLastFocusNode(node);
                var focus = (node as ContainerNode).Desktop.FocusTracker.FocusNode();
                return ParseContainer("Container", node.Direction, myFocus, focus, (node as ContainerNode).Childs);
            }
            else if (t == typeof(WindowNode))
            {
                return ParseLeaf(node as WindowNode);
            }
            else
            {
                throw new InvalidProgramException($"Unknown Node derivation {t.ToString()}");
            }
        }

        protected virtual NodeParseResult ParseContainer(string containerName, Direction direction, Node myFocusNode, Node focusNode, IList<Node> childs)
        {
            var cluster = AllocCluster();
            var objects = new List<GraphObject>();
            var relations = new List<GraphRelation>();
            var clusters  = new List<GraphCluster>();
            cluster.Label = $"{containerName} - {direction.ToString()}";

            ParseChilds(ref cluster, ref objects, ref relations, ref clusters, myFocusNode, focusNode, childs);

            return new NodeParseResult(cluster, objects, relations, clusters);
        }

        protected virtual void ParseChilds(ref GraphCluster cluster, ref List<GraphObject> objects,
            ref List<GraphRelation> relations,
            ref List<GraphCluster> clusters,
            Node myFocusNode, Node focusNode, IList<Node> childs)
        {
            foreach(var c in childs)
            {
                var obj = AllocObject();
                obj.Label = c.ShortName;
                obj.Shape = GraphShapeEnum.Circle;
                if (c == focusNode)
                    obj.FillColor = GraphColor.Red;
                if (c == myFocusNode)
                    obj.Color = GraphColor.Red;

                cluster.Objects.Add(obj);

                var result = ParseNode(c);
                if (result.Element is GraphCluster)
                {
                    var cc = result.Element as GraphCluster;
                    relations.Add(new GraphRelation(obj, cc.RelateToCluster, relateFromObj2Cluster: cc));
                    clusters.Add(cc);
                }
                else
                {
                    objects.Add(result.Element as GraphObject);
                }

                relations.AddRange(result.Relations);
                clusters.AddRange(result.Clusters);
            }
        }

        protected virtual NodeParseResult ParseLeaf(WindowNode node)
        {
            var obj = AllocObject();
            var objects = new List<GraphObject>();
            var relations = new List<GraphRelation>();
            var clusters  = new List<GraphCluster>();
            obj.Label = node.ShortName;

            return new NodeParseResult(obj, objects, relations, clusters);
        }

        protected virtual GraphCluster AllocCluster()
        {
            var cluster = new GraphCluster(_global_cluster_index);
            _global_cluster_index++;
            return cluster;
        }

        protected virtual GraphObject AllocObject()
        {
            var obj = new GraphObject(_global_object_index);
            _global_object_index++;
            return obj;
        }
        
        public string GetOutput()
        {
            var sb = new StringBuilder();
            sb.AppendLine("graph VirtualDesktop {");
            sb.AppendLine("graph [compound=true];");

            foreach(var o in Objects)
                sb.AppendLine($"{o};");
            foreach(var c in Clusters)
                sb.AppendLine($"{c}");
            foreach(var r in Relations)
                sb.AppendLine($"{r};");

            sb.AppendLine("}");
            return sb.ToString();
        }
    }

    public abstract class GraphBase
    {

    }

    public enum GraphShapeEnum
    {
        Circle,
        Box,
        Diamond
    }

    public class GraphCluster: GraphBase
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public IList<GraphObject> Objects { get; set; }
        public IList<GraphCluster> Clusters { get; set; }
        public IList<GraphRelation> Relations { get; set; }

        public GraphObject RelateToCluster
        {
            get
            {
                if (Objects.Count == 0)
                    Objects.Add(new GraphObject(0) { Name = Guid.NewGuid().ToString("N").ToLowerInvariant() });

                return Objects.First();
            }
        }
        public GraphCluster(int index)
        {
            Objects = new List<GraphObject>();
            Clusters = new List<GraphCluster>();
            Relations = new List<GraphRelation>();
            Name = $"cluster_{index}";
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"subgraph {Name} {{");
            if (string.IsNullOrEmpty(Label) == false)
                sb.AppendLine($"label = \"{Label}\";");

            foreach(var o in Objects)
                sb.AppendLine($"{o};");
            foreach(var c in Clusters)
                sb.AppendLine($"{c}");
            foreach(var r in Relations)
                sb.AppendLine($"{r};");

            sb.AppendLine("}");
            return sb.ToString();
        }
    }

    public class GraphRelation
    {
        public GraphObject Obj1 { get; set; }
        public GraphObject Obj2 { get; set; }

        public GraphCluster RelateFromObj1Cluster { get; set; }
        public GraphCluster RelateFromObj2Cluster { get; set; }

        public GraphRelation(GraphObject obj1, GraphObject obj2, GraphCluster relateFromObj1Cluster = null, GraphCluster relateFromObj2Cluster = null)
            : this(obj1, obj2)
        {
            this.RelateFromObj1Cluster = relateFromObj1Cluster;
            this.RelateFromObj2Cluster = relateFromObj2Cluster;
        }
        public GraphRelation(GraphObject obj1, GraphObject obj2)
        {
            Obj1 = obj1;
            Obj2 = obj2;
        }

        public override string ToString()
        {
            var attr = GetAttributes() ?? "";
            attr = (attr.Length > 0 ? " " : "") + attr;
            return $"{Obj1.Name} -- {Obj2.Name}{attr}";
        }

        protected virtual string GetAttributes()
        {
            if (RelateFromObj1Cluster == null && RelateFromObj2Cluster == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder("[");
            var ltailSet = false;
            if (RelateFromObj1Cluster != null)
            {
                sb.Append("ltail=");
                sb.Append(RelateFromObj1Cluster.Name);
                ltailSet = true;
            }
            if (RelateFromObj2Cluster != null)
            {
                if (ltailSet)
                    sb.Append(" ");
                sb.Append("lhead=");
                sb.Append(RelateFromObj2Cluster.Name);
            }

            sb.Append("]");
            return sb.ToString();
        }
    }

    public enum GraphColor
    {
        None,
        Black,
        Red,
        Yellow
    }

    public class GraphObject: GraphBase
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public GraphShapeEnum Shape { get; set; }
        public GraphColor Color { get; set; }
        public GraphColor FillColor { get; set; }
        public GraphObject(int index)
        {
            Name = "Obj" + index;
            FillColor = GraphColor.None;
            Color = GraphColor.None;
        }

        public override string ToString()
        {
             var attr = GetAttributes() ?? "";
            attr = (attr.Length > 0 ? " " : "") + attr;
           return $"{Name}{attr}";
        }

        protected virtual string GetAttributes()
        {
            var sb = new StringBuilder();
            sb.Append("[shape=");
            sb.Append(Shape.ToString().ToLowerInvariant());

            if (string.IsNullOrEmpty(Label) == false)
            {
                sb.Append(" label=\"");
                sb.Append(Label);
                sb.Append("\"");
            }

            if (FillColor != GraphColor.None)
            {
                sb.Append($" style=filled, fillcolor={FillColor.ToString().ToLowerInvariant()}");
            }

            if (Color != GraphColor.None)
            {
                sb.Append($" color={Color.ToString().ToLowerInvariant()}");
            }

            sb.Append("]");
            return sb.ToString();
        }
    }
}