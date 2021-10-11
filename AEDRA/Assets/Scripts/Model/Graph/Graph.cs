using System.Collections.Generic;
using Utils.Enums;
using Model.Common;
using SideCar.Converters;
using SideCar.DTOs;
using Newtonsoft.Json;
using UnityEngine;
using Model.GraphModel.Traversals;

namespace Model.GraphModel
{
    /// <summary>
    /// Class to manage operations and data realted to a Graph
    /// </summary>
    public class Graph : DataStructure
    {
        /// <summary>
        /// Autogenerated Node Id
        /// </summary>
        /// <value>0 when graph is created</value>
        [JsonProperty]
        public static int NodesId{get; set;}

        /// <summary>
        /// Autogenerated Edge id
        /// </summary>
        /// <value>0 when graph is created</value>
        [JsonProperty]
        public static int EdgesId{get; set;}

        /// <summary>
        /// List to store nodes of the graph
        /// </summary>
        public Dictionary<int,GraphNode> Nodes {get; set;}

        /// <summary>
        /// Adjacent matrix of the graph
        /// </summary>
        public Dictionary<int, Dictionary<int, object>> AdjacentMtx { get; set; }

        /// <summary>
        /// Class to convert between NodeDTO and GraphNode
        /// </summary>
        private GraphNodeConverter _nodeConverter;

        /// <summary>
        /// Dictionary to save all the graph traversals implementations
        /// </summary>
        private Dictionary<TraversalEnum, ITraversalGraphStrategy> _traversals;

        public Graph(){
            NodesId = 0;
            EdgesId = 0;
            Nodes = new Dictionary<int, GraphNode>();
            AdjacentMtx = new Dictionary<int, Dictionary<int, object>>();
            _nodeConverter = new GraphNodeConverter();
            _traversals = new Dictionary<TraversalEnum, ITraversalGraphStrategy>() {
                {TraversalEnum.GraphBFS, new BFSTraversalStrategy() },
                {TraversalEnum.GraphDFS, new DFSTraversalStrategy() }
            };
        }

        /// <summary>
        /// Method to add a node on the graph
        /// </summary>
        /// <param name="element"> Node that will be added to the graph </param>
        public override void AddElement(ElementDTO element)
        {
            GraphNodeDTO nodeDTO = (GraphNodeDTO)element;
            GraphNode node = _nodeConverter.ToEntity(nodeDTO);
            node.Id = NodesId++;
            Nodes.Add(node.Id,node);
            AdjacentMtx.Add(node.Id, new Dictionary<int, object>());
            //return DTO updated
            element = _nodeConverter.ToDto(node);
            element.Operation = AnimationEnum.CreateAnimation;
            DataStructure.Notify(element);
            if(nodeDTO.ElementToConnectID!=null){
                ConnectElements(new EdgeDTO(0, 0, (int)nodeDTO.ElementToConnectID, node.Id));
            }
        }

        /// <summary>
        /// Method to remove a node of the graph
        /// </summary>
        /// <param name="element"> Node that will be removed</param>
        public override void DeleteElement(ElementDTO element)
        {
            DeleteEdges(element.Id);
            this.Nodes.Remove( element.Id );
            element.Operation = AnimationEnum.DeleteAnimation;
            DataStructure.Notify(element);
        }

        /// <summary>
        /// Method to do a traversal on the graph
        /// </summary>
        /// <param name="traversalName">Enum of the traversal to execute</param>
        /// <param name="data">Optional parameter to pass the data to the traversal</param>
        public override void DoTraversal(TraversalEnum traversalName, ElementDTO data = null)
        {
            this._traversals[traversalName].DoTraversal(this,data);
        }

        /// <summary>
        /// Method to connect two nodes bidirectionally
        /// </summary>
        /// <param name="EdgeDTO">Information needed for edge creation</param>
        public void ConnectElements(ElementDTO EdgeDTO)
        {
            EdgeDTO edgeDTO = (EdgeDTO) EdgeDTO;
            edgeDTO.Id = EdgesId++;
            // TODO: validar aristas
            bool edgeStartToEnd = AdjacentMtx[edgeDTO.IdStartNode].ContainsKey(edgeDTO.IdEndNode);
            bool edgeEndToStart = AdjacentMtx[edgeDTO.IdEndNode].ContainsKey(edgeDTO.IdStartNode);
            if(!edgeStartToEnd && !edgeEndToStart){
                AdjacentMtx[edgeDTO.IdStartNode].Add(edgeDTO.IdEndNode, edgeDTO.Value);
                AdjacentMtx[edgeDTO.IdEndNode].Add(edgeDTO.IdStartNode, edgeDTO.Value);
                NotifyEdge(edgeDTO.IdStartNode,edgeDTO.IdEndNode,AnimationEnum.CreateAnimation);
            }
            else{
                //TODO: delete this
                Debug.Log("Ya existe la arista");
            }
        }

        /// <summary>
        /// Method to obtain list of neighbors of a given node
        /// </summary>
        /// <param name="nodeId">Id of node to search</param>
        /// <returns>List of ids representing the neighbors of the node</returns>
        public List<int> GetNeighbors(int nodeId){
            List<int> neighbors = new List<int>();
            foreach (int neighbor in AdjacentMtx[nodeId].Keys)
            {
                neighbors.Add(neighbor);
            }
            return neighbors;
        }

        /// <summary>
        /// Create an saved graph
        /// </summary>
        public override void CreateDataStructure()
        {
            Dictionary<int,bool> visited = new Dictionary<int, bool>();
            foreach (GraphNode node in this.Nodes.Values)
            {
                visited.Add(node.Id,false);
                NotifyNode(node.Id, AnimationEnum.CreateAnimation);
            }
            foreach (GraphNode node in this.Nodes.Values)
            {
                visited[node.Id] = true;
                foreach (int key in this.AdjacentMtx[node.Id].Keys)
                {
                    if(!visited[key]){
                        NotifyEdge(node.Id, key,AnimationEnum.CreateAnimation);
                    }
                }
            }
        }

        /// <summary>
        /// Method to obtain list of neighbors of a given node
        /// </summary>
        /// <param name="nodeId">Id of node to search</param>
        /// <returns>List of ids representing the neighbors of the node</returns>
        public void DeleteEdges(int nodeId){
            if(AdjacentMtx.ContainsKey(nodeId))
            {
                foreach (int key in AdjacentMtx.Keys)
                {
                    bool existsStartToEnd = AdjacentMtx[key].Remove(nodeId);
                    bool existsEndToStart = AdjacentMtx[nodeId].Remove(key);
                    if(existsStartToEnd || existsEndToStart){
                       //TODO: Revisar el warning de andres cuando se eliminan nodos
                       NotifyEdge(key,nodeId,AnimationEnum.DeleteAnimation);
                    }
                }
            }
            AdjacentMtx.Remove(nodeId);
        }

        //TODO: This method needs to take into account that a GraphNode may have been deleted
        /// <summary>
        /// Method to notify when a node is modified
        /// </summary>
        /// <param name="id">Id of the modified node</param>
        /// <param name="operation">Operation that was applied to node</param>
        public void NotifyNode(int id, AnimationEnum operation){
            GraphNode node = this.Nodes[id];
            GraphNodeDTO dto = _nodeConverter.ToDto(node);
            dto.Operation = operation;
            Notify(dto);
        }

        /// <summary>
        /// Method to notify when an edge is modified
        /// </summary>
        /// <param name="start">Id node from edge start</param>
        /// <param name="end">Id node from edge ends</param>
        /// <param name="operation">Operations that was applied to node</param>
        public void NotifyEdge(int start, int end, AnimationEnum operation){
            object value = null;
            if(AdjacentMtx[start].ContainsKey(end)){
                value = AdjacentMtx[start][end];
            }
            EdgeDTO edge = new EdgeDTO(0, value, start, end)
            {
                Operation = operation
            };
            NotifyNode(start, AnimationEnum.UpdateAnimation);
            NotifyNode(end, AnimationEnum.UpdateAnimation);
            Notify(edge);
        }

        /// <summary>
        /// Method that update a node
        /// </summary>
        /// <param name="element">Node information to update</param>
        public override void UpdateElement(ElementDTO element)
        {
            if(Nodes.ContainsKey(element.Id)){
                GraphNode node = Nodes[element.Id];
                node.Coordinates = element.Coordinates;
                node.Value = element.Value;
                NotifyNode(node.Id, AnimationEnum.UpdateAnimation);
            }
        }

        public override void DoAlgorithm(AlgorithmEnum algorithmName, ElementDTO data = null)
        {
        }
    }
}