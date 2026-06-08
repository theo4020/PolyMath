using Godot;

namespace MathsPower;

public enum CameraMode { Orbit = 0, Free = 1 }

// Caméra polyvalente :
//   - Orbite : drag gauche = rotation, clic-milieu = pan, molette = zoom.
//   - Libre  : ZQSD/WASD + Espace/Ctrl, Maj = rapide, clic droit = regarder,
//              molette = vitesse.
//   - Projection orthographique ↔ perspective.
//   - Rotation automatique (traveling) à vitesse réglable.
//   - Transitions douces vers les vues préréglées (Face / Dessus / 3-quarts).
[GlobalClass]
public partial class OrbitCamera : Camera3D
{
    [Export] public float Distance = 5.0f;

    private CameraMode _mode = CameraMode.Orbit;
    private float _yaw = 0.6f;
    private float _pitch = 0.5f;
    private Vector3 _target = Vector3.Zero;  
    private Vector3 _freePos = Vector3.Zero;  

    private bool _autoRotate;
    private float _autoRotateSpeed = 0.4f;   
    private float _moveSpeed = 4.0f;         

    // Transition douce vers une vue préréglée.
    private bool _hasGoal;
    private float _goalYaw, _goalPitch, _goalDistance;

    public override void _Ready()
    {
        _freePos = _target + Offset();
        UpdateTransform();
    }

    // Vecteurs dérivés de yaw/pitch
    private Vector3 OffsetUnit()
    {
        float sp = Mathf.Sin(_pitch), cp = Mathf.Cos(_pitch);
        float sy = Mathf.Sin(_yaw), cy = Mathf.Cos(_yaw);
        return new Vector3(cp * sy, sp, cp * cy);
    }
    private Vector3 Offset() => OffsetUnit() * Distance;
    private Vector3 Forward() => -OffsetUnit(); // direction

    // Boucle
    public override void _Process(double delta)
    {
        float dt = (float)delta;
        bool dirty = false;

        if (_autoRotate)
        {
            _yaw += _autoRotateSpeed * dt;
            dirty = true;
        }

        if (_hasGoal && _mode == CameraMode.Orbit)
        {
            float a = 1.0f - Mathf.Exp(-9.0f * dt);
            _yaw = Mathf.Lerp(_yaw, _goalYaw, a);
            _pitch = Mathf.Lerp(_pitch, _goalPitch, a);
            Distance = Mathf.Lerp(Distance, _goalDistance, a);
            if (Mathf.Abs(_yaw - _goalYaw) < 0.001f && Mathf.Abs(_pitch - _goalPitch) < 0.001f)
            {
                _yaw = _goalYaw; _pitch = _goalPitch; Distance = _goalDistance;
                _hasGoal = false;
            }
            dirty = true;
        }

        if (_mode == CameraMode.Free)
        {
            Vector3 fwd = Forward();
            Vector3 right = fwd.Cross(Vector3.Up).Normalized();
            Vector3 move = Vector3.Zero;
            if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Z)) move += fwd;
            if (Input.IsKeyPressed(Key.S)) move -= fwd;
            if (Input.IsKeyPressed(Key.D)) move += right;
            if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Q)) move -= right;
            if (Input.IsKeyPressed(Key.Space)) move += Vector3.Up;
            if (Input.IsKeyPressed(Key.Ctrl)) move -= Vector3.Up;
            if (move != Vector3.Zero)
            {
                float boost = Input.IsKeyPressed(Key.Shift) ? 3.0f : 1.0f;
                _freePos += move.Normalized() * _moveSpeed * boost * dt;
                dirty = true;
            }
        }

        if (dirty)
            UpdateTransform();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            Vector2 d = motion.Relative;
            if (_mode == CameraMode.Orbit)
            {
                if (Input.IsMouseButtonPressed(MouseButton.Left))
                {
                    _hasGoal = false;
                    _yaw -= d.X * 0.01f;
                    _pitch = Mathf.Clamp(_pitch - d.Y * 0.01f, -1.5f, 1.5f);
                    UpdateTransform();
                }
                else if (Input.IsMouseButtonPressed(MouseButton.Middle))
                {
                    PanView(d);
                }
            }
            else // Free
            {
                if (Input.IsMouseButtonPressed(MouseButton.Right))
                {
                    _yaw -= d.X * 0.005f;
                    _pitch = Mathf.Clamp(_pitch - d.Y * 0.005f, -1.5f, 1.5f);
                    UpdateTransform();
                }
            }
        }
        else if (@event is InputEventMouseButton b && b.Pressed)
        {
            if (b.ButtonIndex == MouseButton.WheelUp)
            {
                if (_mode == CameraMode.Orbit) { Distance = Mathf.Max(Distance * 0.9f, 0.5f); UpdateTransform(); }
                else { _moveSpeed = Mathf.Min(_moveSpeed * 1.15f, 40.0f); }
            }
            else if (b.ButtonIndex == MouseButton.WheelDown)
            {
                if (_mode == CameraMode.Orbit) { Distance = Mathf.Min(Distance * 1.1f, 60.0f); UpdateTransform(); }
                else { _moveSpeed = Mathf.Max(_moveSpeed * 0.87f, 0.3f); }
            }
        }
    }

    private void UpdateTransform()
    {
        if (_mode == CameraMode.Orbit)
        {
            Position = _target + Offset();
            LookAt(_target);
        }
        else
        {
            Position = _freePos;
            LookAt(_freePos + Forward());
        }
    }

    private void PanView(Vector2 deltaPixels)
    {
        float viewportH = Mathf.Max(GetViewport().GetVisibleRect().Size.Y, 1.0f);
        float scale = (Projection == ProjectionType.Orthogonal ? Size : Distance) / viewportH;
        Basis basis = GlobalTransform.Basis;
        Vector3 pan = basis.Column0 * (-deltaPixels.X * scale) + basis.Column1 * (deltaPixels.Y * scale);
        _target += pan;
        UpdateTransform();
    }

    // API publique (menu caméra)
    public void SetMode(int mode)
    {
        var newMode = (CameraMode)mode;
        if (newMode == _mode) return;
        if (newMode == CameraMode.Free)
            _freePos = _target + Offset();              // continuité orbite→libre
        else
            _target = _freePos + Forward() * Distance;  // continuité libre→orbite
        _mode = newMode;
        _hasGoal = false;
        UpdateTransform();
    }

    public void SetPerspective(bool perspective)
    {
        Projection = perspective ? ProjectionType.Perspective : ProjectionType.Orthogonal;
    }

    public void SetAutoRotate(bool on) => _autoRotate = on;
    public void SetAutoRotateSpeed(float s) => _autoRotateSpeed = s;
    public void SetMoveSpeed(float s) => _moveSpeed = s;
    public void SetFieldOfView(float fov) => Fov = fov;
    public void SetOrthoSize(float size) => Size = size;

    public float CurrentFov => Fov;
    public bool IsPerspective => Projection == ProjectionType.Perspective;

    public void ResetView()
    {
        _target = Vector3.Zero;
        Distance = 5.0f;
        GoTo(0.6f, 0.5f, 5.0f);
    }

    public void ViewFace() => GoTo(0.0f, 0.0f, Distance);
    public void ViewTop() => GoTo(0.0f, Mathf.Pi / 2.0f - 0.001f, Distance);
    public void ViewThreeQuarters() => GoTo(0.6f, 0.5f, Distance);

    // Lance une transition douce (sauf en mode libre où l'on saute).
    private void GoTo(float yaw, float pitch, float distance)
    {
        if (_mode == CameraMode.Free)
        {
            _yaw = yaw; _pitch = pitch; Distance = distance;
            _target = Vector3.Zero;
            _freePos = _target + Offset();
            UpdateTransform();
            return;
        }
        _goalYaw = yaw; _goalPitch = pitch; _goalDistance = distance;
        _hasGoal = true;
    }
}
