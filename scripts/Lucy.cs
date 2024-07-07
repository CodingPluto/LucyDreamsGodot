using Godot;
using System;
using System.Numerics;
using System.Reflection.Metadata;
using System.Diagnostics; 


public partial class Lucy : CharacterBody2D
{
    static readonly float Speed;
    static readonly float JumpVelocity;
    static readonly int CoyoteFrames;
    static readonly int DeathFrames;
    static readonly float Gravity;
    bool _isAlive;
    bool _isSuperJumping;
    bool _hasSuperJumped;
    int _deathFrameCount;
    int _coyoteFrameCount;
    bool _coyoteTime;
    Platform _currentPlatform;
    AnimatedSprite2D _sprite;
    CollisionShape2D _hitbox;
    Camera2D _camera;
    bool _hasJumped;
    bool _isJumping;
    bool _isRunning;
    Godot.Vector2 _bufferPosition;
    Godot.Vector2 _bufferVelocity;
    private PhysicsState _physicsState;
    enum PhysicsState
    {
        CLOUD_INTERACTION, GENERAL_INTERACTION, NO_PHYSICS
    }

    static Lucy()
    {
        Speed = 300.0f;
        JumpVelocity = -800.0f;
        CoyoteFrames = 60;
        DeathFrames = 40;
        Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    }
    Lucy()
    {
        _isAlive = true;
        _deathFrameCount = 0;
        _isRunning = false;
        _hasJumped = false;
        _isJumping = false;
        _hasSuperJumped = false;
        _isSuperJumping = false;
        _coyoteFrameCount = 0;
    }
    
    public override void _Ready()
    {
        Position = new Godot.Vector2(0, 0);
        ZIndex = 10;
        _sprite = GetNode<AnimatedSprite2D>("Sprite");
        Debug.Assert(_sprite != null);
        _hitbox = GetNode<CollisionShape2D>("Hitbox");
        Debug.Assert(_hitbox != null);
        _camera = GetNode<Camera2D>("Camera");
        Debug.Assert(_camera != null);
        _camera.LimitRight = (int)GetViewportRect().Size.X / 2;
        _camera.LimitLeft = -_camera.LimitRight;
        _camera.LimitBottom = (int)GetViewportRect().Size.Y / 2;
        _physicsState = PhysicsState.GENERAL_INTERACTION;
    }
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!_isAlive)
        {
            Die();
        }

        if (Input.IsActionJustPressed("Exit"))
        {
            GetTree().Quit();
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        _bufferVelocity = Velocity;
        _bufferPosition = Position;
        if (_isSuperJumping && _bufferVelocity.Y > 0)
        {
            _isSuperJumping = false;
        }
        switch (_physicsState)
        {
            case PhysicsState.CLOUD_INTERACTION:
                PhysicsCloudInteraction(ref delta);
                break;
            case PhysicsState.GENERAL_INTERACTION:
                GeneralPlatformInteraction(ref delta);
                break;
            case PhysicsState.NO_PHYSICS:
                return;
            default:
                GD.Print("This isn't supposed to trigger!");
                break;
        }
        PollJump(ref delta);
        Godot.Vector2 direction = Input.GetVector("lucy_left", "lucy_right", "lucy_up", "lucy_down");
        if (direction != Godot.Vector2.Zero)
        {
            _bufferVelocity.X = direction.X * Speed;
            _isRunning = true;
        }
        else
        {
            _bufferVelocity.X = Mathf.MoveToward(Velocity.X, 0, Speed / 30);
            _isRunning = false;
        }
        if (direction.X > 0)
        {
            _sprite.FlipH = false;
        }
        else if (direction.X < 0)
        {
            _sprite.FlipH = true;
        }
        if (_isJumping)
        {
            _sprite.Play("jumping");
        }
        else if (Velocity.Y >= 0)
        {
            if (Math.Abs(_bufferVelocity.X) > 50 && HasPlatformInteraction())
            {
                _sprite.Play("running");
            }
            else if (HasPlatformInteraction())
            {
                _sprite.Play("idle");
            }
            else if (!_hasJumped || (_hasJumped && _bufferVelocity.Y > 850))
            {
                _sprite.Play("falling");
            }
        }
        Position = _bufferPosition;
        Velocity = _bufferVelocity;
        MoveAndSlide();
        float leftBound = -GetViewportRect().Size.X / 2 + (_hitbox.Shape.GetRect().Size.X) * Scale.X;
        float rightBound = GetViewportRect().Size.X / 2 - (_hitbox.Shape.GetRect().Size.X) * Scale.X;
        _bufferPosition = Position;
        if (_bufferPosition.X > rightBound)
        {
            _bufferPosition.X = rightBound;
        }
        else if (_bufferPosition.X < leftBound)
        {
            _bufferPosition.X = leftBound;
        }
        Position = _bufferPosition;
    }
    private bool HasPlatformInteraction()
    {
        return IsOnFloor() || _physicsState == PhysicsState.CLOUD_INTERACTION;
    }
    private void PollJump(ref double delta)
    {
        if (Input.IsActionPressed("lucy_up") || _hasSuperJumped)
        {
            _sprite.Play("jumping");
            _bufferVelocity.Y = JumpVelocity;
            _hasJumped = true;
            _isJumping = true;
            _physicsState = PhysicsState.GENERAL_INTERACTION;
        }
        if (HasPlatformInteraction() && _hasJumped)
        {
            _hasJumped = false;
        }
        if (_hasSuperJumped)
        {
            _isSuperJumping = true;
            _bufferVelocity.Y *= 2.5f;
            _hasSuperJumped = false;
        }
    }
    private void PhysicsCloudInteraction(ref double delta)
    {
        float widthAdjustment = _hitbox.Shape.GetRect().Size.X * Scale.X / 2;
        float platformWidthAdjustment = _currentPlatform.spriteWidth / 2;
        _bufferVelocity.Y = 0;
        float newPositionX = _bufferPosition.X + _currentPlatform.velocity.X;
        float newPositionY = _currentPlatform.Position.Y - _currentPlatform.spriteHeight / 2 - _hitbox.Shape.GetRect().Size.Y * Scale.Y / 2;
        _bufferPosition = new Godot.Vector2(newPositionX, newPositionY);
        bool withinXBounds = (_bufferPosition.X + widthAdjustment > _currentPlatform.Position.X - platformWidthAdjustment)
        && (_bufferPosition.X - widthAdjustment < _currentPlatform.Position.X + platformWidthAdjustment);
        if (!withinXBounds)
        {
            _physicsState = PhysicsState.GENERAL_INTERACTION;
            _currentPlatform.ResetZIndex();
            _currentPlatform.interactingWithPlayer = false;
        }

    }
    private void GeneralPlatformInteraction(ref double delta)
    {
        _isJumping = false;
        if (!IsOnFloor())
        {
            _bufferVelocity.Y += Gravity * (float)delta;
        }
        else
        {
            _coyoteFrameCount = 0;
            _bufferVelocity.Y = 0;
            for (int i = 0; i < GetSlideCollisionCount(); i++)
            {
                KinematicCollision2D collision = GetSlideCollision(i);
                Node baseNode = (Node)collision.GetCollider();
                int ID = 0;
                bool isNumber = int.TryParse(baseNode.Name.ToString(), out ID);
                if (isNumber)
                { // Identifying collider
                    _currentPlatform = (Platform)baseNode;
                    _physicsState = PhysicsState.CLOUD_INTERACTION;
                    _currentPlatform.BringToFront();
                    _currentPlatform.interactingWithPlayer = true;
                }
            }
        }
    }
    public void OnAreaEntered(Area2D area)
    {
        GD.Print("Area Name: ", area.Name);
        if (area.Name.ToString().Substring(0, 5) == "Super")
        {
            _hasSuperJumped = true;
        }
        else if (!_isSuperJumping)
        {
            _isAlive = false;
        }
    }
    private void Die()
    {
        if (_deathFrameCount == 0)
        {
            _physicsState = PhysicsState.NO_PHYSICS;
            Velocity = new Godot.Vector2(0, 0);
            _sprite.Play("die");
        }
        if (_deathFrameCount > DeathFrames)
        {
            Position = new Godot.Vector2(0, 0);
            Velocity = new Godot.Vector2(0, 0);
            _isAlive = true;
            _deathFrameCount = 0;
            _physicsState = PhysicsState.GENERAL_INTERACTION;
            return;
        }
        _deathFrameCount++;
    }
}
