using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public class SD_DroneTrailProperties
    {
        public string texturePath;
        public float width = 0.2f;
        public int length = 30;
        public Color color = Color.white;
        public bool colorOverTime = true;
        public float renderYOffset = -0.1f;
        public float uvScale = 1f;
        public float fadePower = 1f;
        public float minSpeed = 0f;
        public float cutoffSpeed = -1f;
        public Vector3 localOffset = Vector3.zero;
        public SimpleCurve widthCurve;
        public SimpleCurve alphaCurve;

        public Material TrailMaterial
        {
            get
            {
                if (string.IsNullOrEmpty(texturePath))
                {
                    return BaseContent.ClearMat;
                }

                return MaterialPool.MatFrom(texturePath, ShaderDatabase.MoteGlow, color);
            }
        }
    }
}
