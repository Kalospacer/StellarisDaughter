using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public class SD_DroneTrailRenderer
    {
        private struct TrailPoint
        {
            public Vector3 Pos;
            public int CreationTick;
            public bool Broken;
        }

        private readonly SD_DroneEntity drone;
        private readonly SD_DroneTrailProperties props;
        private readonly LinkedList<TrailPoint> points;
        private readonly List<Vector3> vertices;
        private readonly List<Vector2> uvs;
        private readonly List<Color> colors;
        private readonly List<int> triangles;
        private readonly List<float> dists;

        private Material material;
        private Mesh mesh;
        private bool isEmitting;

        public SD_DroneTrailRenderer(SD_DroneEntity drone, SD_DroneTrailProperties props)
        {
            this.drone = drone;
            this.props = props;
            points = new LinkedList<TrailPoint>();
            vertices = new List<Vector3>(props.length * 2);
            uvs = new List<Vector2>(props.length * 2);
            colors = new List<Color>(props.length * 2);
            triangles = new List<int>(props.length * 6);
            dists = new List<float>(props.length);
        }

        public void Tick()
        {
            if (drone.Map == null)
            {
                return;
            }

            var ticksGame = Find.TickManager.TicksGame;
            while (points.Count > 0 && ticksGame - points.First.Value.CreationTick > props.length)
            {
                points.RemoveFirst();
            }

            var drawPos = drone.DrawPos;
            if (props.localOffset != Vector3.zero)
            {
                var rotation = Quaternion.LookRotation(drone.RealDir == Vector3.zero ? Vector3.forward : drone.RealDir);
                drawPos += rotation * props.localOffset;
            }

            var startSpeed = props.minSpeed;
            var stopSpeed = props.cutoffSpeed >= 0f ? props.cutoffSpeed : props.minSpeed;
            var speedPerSecond = drone.Velocity.magnitude * 60f;
            var forceBreak = false;

            if (isEmitting)
            {
                if (speedPerSecond < stopSpeed)
                {
                    isEmitting = false;
                    return;
                }
            }
            else
            {
                if (speedPerSecond < startSpeed || speedPerSecond <= 0.001f)
                {
                    return;
                }

                isEmitting = true;
                forceBreak = true;
            }

            if (points.Count == 0 || forceBreak)
            {
                points.AddLast(new TrailPoint
                {
                    Pos = drawPos,
                    CreationTick = ticksGame,
                    Broken = forceBreak
                });
                return;
            }

            if ((points.Last.Value.Pos - drawPos).sqrMagnitude > 0.01f)
            {
                points.AddLast(new TrailPoint
                {
                    Pos = drawPos,
                    CreationTick = ticksGame,
                    Broken = false
                });
            }
        }

        public void Draw(Vector3 drawLoc)
        {
            if (!Find.CameraDriver.CurrentViewRect.ExpandedBy(5).Contains(drawLoc.ToIntVec3()) || (points.Count < 2 && !isEmitting))
            {
                return;
            }

            mesh ??= new Mesh();
            mesh.MarkDynamic();
            material ??= props.TrailMaterial;
            UpdateMesh(drawLoc);
            Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0);
        }

        private void UpdateMesh(Vector3 headPos)
        {
            if (mesh == null)
            {
                return;
            }

            mesh.Clear();
            if (points.Count == 0 && !isEmitting)
            {
                return;
            }

            var pointCount = points.Count + (isEmitting ? 1 : 0);
            if (pointCount < 2)
            {
                return;
            }

            vertices.Clear();
            uvs.Clear();
            colors.Clear();
            triangles.Clear();
            dists.Clear();

            var totalDist = 0f;
            var node = points.First;
            for (var i = 0; i < pointCount; i++)
            {
                if (node != null)
                {
                    if (node.Value.Broken)
                    {
                        totalDist = 0f;
                    }
                    else if (i > 0)
                    {
                        totalDist += (node.Value.Pos - node.Previous.Value.Pos).magnitude;
                    }

                    dists.Add(totalDist);
                    node = node.Next;
                }
                else
                {
                    if (i > 0)
                    {
                        totalDist += (headPos - points.Last.Value.Pos).magnitude;
                    }

                    dists.Add(totalDist);
                }
            }

            var ticksGame = Find.TickManager.TicksGame;
            node = points.First;
            for (var i = 0; i < pointCount; i++)
            {
                Vector3 pos;
                var creationTick = ticksGame;
                var broken = false;

                if (node != null)
                {
                    pos = node.Value.Pos;
                    creationTick = node.Value.CreationTick;
                    broken = node.Value.Broken;
                }
                else
                {
                    pos = headPos;
                }

                var forward = Vector3.forward;
                var nextBroken = false;
                if (i < pointCount - 1)
                {
                    Vector3 nextPos;
                    if (node != null && node.Next != null)
                    {
                        nextPos = node.Next.Value.Pos;
                        nextBroken = node.Next.Value.Broken;
                    }
                    else
                    {
                        nextPos = headPos;
                    }

                    if (!nextBroken)
                    {
                        forward = (nextPos - pos).normalized;
                    }
                    else if (i > 0)
                    {
                        var prevPos = node != null && node.Previous != null ? node.Previous.Value.Pos : Vector3.zero;
                        if (prevPos != Vector3.zero)
                        {
                            forward = (pos - prevPos).normalized;
                        }
                    }
                }
                else if (i > 0)
                {
                    var prevPos = node?.Previous.Value.Pos ?? points.Last.Value.Pos;
                    forward = (pos - prevPos).normalized;
                }

                if (forward == Vector3.zero)
                {
                    forward = Vector3.forward;
                }

                var side = Vector3.Cross(Vector3.up, forward).normalized;
                var ageTicks = ticksGame - creationTick;
                var ageRatio = Mathf.Clamp01(ageTicks / (float)props.length);
                var widthFactor = props.widthCurve != null ? props.widthCurve.Evaluate(1f - ageRatio) : 1f;
                var halfWidth = props.width * widthFactor * 0.5f;
                var vertexPos = pos;
                vertexPos.y += props.renderYOffset;
                vertices.Add(vertexPos - side * halfWidth);
                vertices.Add(vertexPos + side * halfWidth);

                var dist = dists[i];
                var uvX = props.uvScale > 1f ? dist * props.uvScale : ageRatio;
                uvs.Add(new Vector2(uvX, 0f));
                uvs.Add(new Vector2(uvX, 1f));

                var color = props.color;
                if (props.colorOverTime)
                {
                    var remaining = 1f - ageRatio;
                    var alpha = props.alphaCurve != null
                        ? props.alphaCurve.Evaluate(remaining)
                        : Mathf.Pow(Mathf.Max(0f, remaining), props.fadePower);
                    color.a *= alpha;
                }

                colors.Add(color);
                colors.Add(color);

                if (i < pointCount - 1 && !nextBroken)
                {
                    var index = i * 2;
                    triangles.Add(index);
                    triangles.Add(index + 2);
                    triangles.Add(index + 1);
                    triangles.Add(index + 2);
                    triangles.Add(index + 3);
                    triangles.Add(index + 1);
                }

                if (node != null)
                {
                    node = node.Next;
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }
    }
}
