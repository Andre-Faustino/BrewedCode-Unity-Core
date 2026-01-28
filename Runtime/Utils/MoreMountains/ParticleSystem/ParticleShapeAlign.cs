#if MOREMOUNTAINS_TOOLS
using MoreMountains.Tools;
using UnityEngine;

namespace BrewedCode.Utils.MoreMountains
{
    //Align the shape of Particles since it doesn't exactly follow rotation
    [DisallowMultipleComponent]
    public class ParticleShapeAlign : MMMonoBehaviour
    {
        public enum SourceMode
        {
            Transform,
            LineRenderer
        }

        public enum AimAxis
        {
            Up,
            Right
        } // Qual eixo local vai apontar para a direção da linha

        public enum ApplySpace
        {
            World,
            Local
        } // Aplicar em rotation (world) ou localRotation

        [GroupPreset(InspectorGroupPreset.Settings)]
        [Header("Source")]
        [SerializeField]
        private SourceMode source = SourceMode.Transform;

        [SerializeField]
        [MMEnumCondition("source", (int)SourceMode.Transform)]
        private Transform reference; // usado quando Source = Transform

        [SerializeField]
        [MMEnumCondition("source", (int)SourceMode.LineRenderer)]
        private LineRenderer line; // usado quando Source = LineRenderer

        [SerializeField, Tooltip("0 ou 1")]
        [MMEnumCondition("source", (int)SourceMode.LineRenderer)]
        private int pointIndex; // 0 = do ponto 0 para 1; 1 = do ponto 1 para 0

        [Header("Orientation")]
        [SerializeField] private AimAxis aimAxis = AimAxis.Up;
        [SerializeField] private ApplySpace applySpace = ApplySpace.Local;
        [SerializeField, Tooltip("Rotação extra após alinhar à direção")]
        private Vector3 eulerOffset = Vector3.zero;

        void LateUpdate()
        {
            if (source == SourceMode.Transform)
            {
                if (!reference) return;
                ApplyRotation(reference.rotation);
                return;
            }

            // Source: LineRenderer
            if (!line || !line.enabled) return;
            if (line.positionCount < 2) return;

            // Pega posições em espaço de MUNDO, independente do useWorldSpace
            Vector3 p0 = GetWorldPosition(line, 0);
            Vector3 p1 = GetWorldPosition(line, 1);

            // Direção: se pointIndex == 0 => 0→1; se == 1 => 1→0
            Vector3 dir = (pointIndex == 0 ? (p1 - p0) : (p0 - p1));
            if (dir.sqrMagnitude < 0.000001f) return;
            dir.Normalize();

            // Constrói a rotação-alvo em world space
            Quaternion worldRot;
            switch (aimAxis)
            {
                case AimAxis.Right:
                    // Eixo local X aponta na direção; Y fica 'up'
                    // Para 2D, forward = Vector3.forward
                    worldRot = Quaternion.LookRotation(Vector3.forward, Vector3.Cross(Vector3.forward, dir));
                    break;

                case AimAxis.Up:
                default:
                    // Eixo local Y aponta na direção
                    worldRot = Quaternion.LookRotation(Vector3.forward, dir);
                    break;
            }

            worldRot *= Quaternion.Euler(eulerOffset);

            ApplyRotation(worldRot);
        }

        private void ApplyRotation(Quaternion worldRot)
        {
            if (applySpace == ApplySpace.World)
            {
                transform.rotation = worldRot;
            }
            else
            {
                transform.localRotation = worldRot;
            }
        }

        private static Vector3 GetWorldPosition(LineRenderer lr, int index)
        {
            var p = lr.GetPosition(index);
            return lr.useWorldSpace ? p : lr.transform.TransformPoint(p);
        }
    }
}
#endif
