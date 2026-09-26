using UnityEngine;

// Ground-projected detection cone. Builds a fan mesh that follows the guard's actual vision cone,
// is clipped by walls (each ray is cast against the sight blockers) and drapes over the ground /
// low cover beneath it. Functionally the GDD's "cone-shaped projector decal" (the URP Decal Projector
// needs the Decal renderer feature and can't be clipped by geometry) with no renderer setup.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GuardConeVisual : MonoBehaviour
{
    public GuardVision vision;
    public int   segments     = 22;
    public float groundOffset = 0.07f;
    public float cullDistance = 45f;

    Mesh _mesh;
    Vector3[] _verts;
    int[]     _tris;
    MeshRenderer _renderer;
    Color[] _cols;
    Color _color = new Color(0.4f, 0.9f, 0.4f, 0.22f);
    bool  _colorDirty = true;
    Camera _cam;


    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _mesh = new Mesh { name = "GuardCone" };
        _mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = _mesh;
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;

        _verts = new Vector3[segments + 2];
        _tris  = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            _tris[i * 3]     = 0;
            _tris[i * 3 + 1] = i + 1;
            _tris[i * 3 + 2] = i + 2;
        }
        _cols = new Color[segments + 2];
        _mesh.vertices = new Vector3[segments + 2];
        _mesh.triangles = _tris;
    }

    public void SetColor(Color c)
    {
        _color = c;
        _colorDirty = true;
    }

    void LateUpdate()
    {
        if (vision == null) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam != null && (_cam.transform.position - transform.position).sqrMagnitude > cullDistance * cullDistance)
        {
            _renderer.enabled = false;
            return;
        }
        _renderer.enabled = true;

        if (_colorDirty)
        {
            // strongest at the guard, fading toward the far edge of the cone
            _cols[0] = _color;
            var far = new Color(_color.r, _color.g, _color.b, _color.a * 0.15f);
            for (int i = 1; i < _cols.Length; i++) _cols[i] = far;
            _mesh.colors = _cols;
            _colorDirty = false;
        }

        Transform eyeT = vision.eyePoint != null ? vision.eyePoint : vision.transform;
        Vector3 eye = eyeT.position;
        Vector3 fwd = eyeT.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) return;
        fwd.Normalize();

        float half = vision.coneAngle * 0.5f;
        float range = vision.maxRange;
        int sightMask = GameLayers.SightBlockers;
        int groundMask = GameLayers.Solid;

        // Origin at the guard's feet
        Vector3 origin = transform.position;
        origin.y += groundOffset;
        _verts[0] = transform.InverseTransformPoint(origin);

        for (int i = 0; i <= segments; i++)
        {
            float a = Mathf.Lerp(-half, half, i / (float)segments);
            Vector3 dir = Quaternion.AngleAxis(a, Vector3.up) * fwd;

            float d = range;
            if (Physics.Raycast(eye, dir, out RaycastHit hit, range, sightMask, QueryTriggerInteraction.Ignore))
                d = Mathf.Max(0.05f, hit.distance - 0.05f);

            Vector3 p = eye + dir * d;
            // drop to the ground under that point
            if (Physics.Raycast(new Vector3(p.x, eye.y + 0.5f, p.z), Vector3.down, out RaycastHit g, 14f, groundMask, QueryTriggerInteraction.Ignore))
                p.y = g.point.y + groundOffset;
            else
                p.y = origin.y;

            _verts[i + 1] = transform.InverseTransformPoint(p);
        }

        _mesh.vertices = _verts;
        _mesh.RecalculateBounds();
    }
}
