using Godot;
using System;
using System.Numerics;
using System.Reflection.Metadata;
using System.Diagnostics;
using System.Collections.Generic;
using System.Buffers;


public partial class Lucy : CharacterBody2D
{
    static readonly float InstantVelocity;
    static readonly float MaximumRunSpeed;
    public static readonly float JumpVelocity;
    static readonly int DeathFrames;
    static readonly float Gravity;
    static readonly int CoyoteFrames;
    static readonly int JumpBufferFrames;
    public CollisionShape2D Hitbox;
    bool _isAlive;
    bool _isSuperJumping;
    bool _hasSuperJumped;
    int _deathFrameCount;
    int _coyoteFrameCount;
    bool _coyoteTime;
    bool _jumpBuffer;
    int _jumpBufferCount;
    Platform _currentPlatform;
    AnimatedSprite2D _sprite;
    Camera2D _camera;
    AudioStreamPlayer _deathSound;
    AudioStreamPlayer _superJumpSound;
    bool _hasJumped;
    bool _isJumping;
    bool _isRunning;
    bool _runningDirectionChanged;
    float _previousXDirectionNormal;
    int _framesRunning;
    bool _isRespawning;
    bool _isLevelingUp;
    Godot.Vector2 _bufferPosition;
    Godot.Vector2 _bufferVelocity;
    private PhysicsState _physicsState;
    enum PhysicsState
    {
        CLOUD_INTERACTION, GENERAL_INTERACTION, NO_PHYSICS
    }

    static Lucy()
    {
        MaximumRunSpeed = 300.0f;
        InstantVelocity = 50.0f;
        JumpVelocity = -900.0f;
        DeathFrames = 40;
        CoyoteFrames = 8;
        JumpBufferFrames = 3;
        Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    }
    Lucy()
    {
        _previousXDirectionNormal = 0;
        _isAlive = true;
        _deathFrameCount = 0;
        _isRunning = false;
        _hasJumped = false;
        _isJumping = false;
        _hasSuperJumped = false;
        _isSuperJumping = false;
        _coyoteFrameCount = 0;
        _coyoteTime = false;
        _isRespawning = false;
        _isLevelingUp = false;
        _jumpBuffer = false;
        _jumpBufferCount = 0;
        _runningDirectionChanged = false;
        _framesRunning = 0;
    }

    public override void _Ready()
    {
        _bufferVelocity = Velocity;
        _bufferPosition = Position;
        ZIndex = 10;
        _sprite = GetNode<AnimatedSprite2D>("Sprite");
        Debug.Assert(_sprite != null);
        Hitbox = GetNode<CollisionShape2D>("Hitbox");
        Debug.Assert(Hitbox != null);
        _camera = GetNode<Camera2D>("Camera");
        Debug.Assert(_camera != null);
        _deathSound = GetNode<AudioStreamPlayer>("DeathSound");
        Debug.Assert(_deathSound != null);
        _superJumpSound = GetNode<AudioStreamPlayer>("SuperJumpSound");
        Debug.Assert(_superJumpSound != null);
        _camera.LimitRight = (int)GetViewportRect().Size.X / 2;
        _camera.LimitLeft = -_camera.LimitRight;
        _camera.LimitBottom = (int)GetViewportRect().Size.Y / 2;
        _camera.LimitTop = -_camera.LimitBottom;
        _physicsState = PhysicsState.GENERAL_INTERACTION;
    }
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!_isAlive && !_isRespawning)
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
                break;
            default:
                GD.Print("This isn't supposed to trigger!");
                break;
        }
        if (_physicsState != PhysicsState.NO_PHYSICS)
        {
            DirectionalPhysics(ref delta);
        }
        StateTriggers();
        Position = _bufferPosition;
        Velocity = _bufferVelocity;
        MoveAndSlide();
        float leftBound = -GetViewportRect().Size.X / 2 + (Hitbox.Shape.GetRect().Size.X) * Scale.X;
        float rightBound = GetViewportRect().Size.X / 2 - (Hitbox.Shape.GetRect().Size.X) * Scale.X;
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
        if (((Input.IsActionPressed("lucy_up") || _jumpBuffer) && (HasPlatformInteraction() || _coyoteTime)) || _hasSuperJumped)
        {
            _sprite.Play("jumping");
            _bufferVelocity.Y = JumpVelocity;
            _hasJumped = true;
            _isJumping = true;
            _physicsState = PhysicsState.GENERAL_INTERACTION;
            _coyoteTime = false;
            _jumpBuffer = false;
        }
        if (HasPlatformInteraction() && _hasJumped)
        {
            _hasJumped = false;
        }
        if (_hasSuperJumped)
        {
            _superJumpSound.Play();
            _isSuperJumping = true;
            _bufferVelocity.Y *= 1.5f;
            _hasSuperJumped = false;
        }
    }
    private void PhysicsCloudInteraction(ref double delta)
    {
        _isJumping = false;
        _coyoteTime = false;
        _coyoteFrameCount = 0;
        float widthAdjustment = Hitbox.Shape.GetRect().Size.X * Scale.X / 2;
        float platformWidthAdjustment = _currentPlatform.SpriteWidth / 2;
        _bufferVelocity.Y = 0;
        float newPositionX = _bufferPosition.X + _currentPlatform.Velocity.X;
        float newPositionY = _currentPlatform.Position.Y - _currentPlatform.SpriteHeight / 2 - Hitbox.Shape.GetRect().Size.Y * Scale.Y / 2;
        _bufferPosition = new Godot.Vector2(newPositionX, newPositionY);
        bool withinXBounds = (_bufferPosition.X + widthAdjustment > _currentPlatform.Position.X - platformWidthAdjustment)
        && (_bufferPosition.X - widthAdjustment < _currentPlatform.Position.X + platformWidthAdjustment);
        if (!withinXBounds)
        {
            _physicsState = PhysicsState.GENERAL_INTERACTION;
            _coyoteTime = true;
            _currentPlatform.InteractingWithPlayer = false;
        }

    }
    private void GeneralPlatformInteraction(ref double delta)
    {
        if (!IsOnFloor())
        {
            _bufferVelocity.Y += Gravity * (float)delta;
            if (Input.IsActionPressed("lucy_up"))
            {
                _jumpBuffer = true;
                _jumpBufferCount = 0;
            }
        }
        else
        {
            _isJumping = false;
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
                    _currentPlatform.InteractingWithPlayer = true;
                }
            }
        }
    }
    public void OnAreaEntered(Area2D area)
    {
        GD.Print("Jumping: ", _isJumping);
        if (area.Name.ToString().Substring(0, 5) == "Super")
        {
            _hasSuperJumped = true;
        }
        else if (!_isSuperJumping && !_isJumping)
        {
            _isAlive = false;
        }
    }
    private void Die()
    {
        if (_deathFrameCount == 0)
        {
            _physicsState = PhysicsState.NO_PHYSICS;
            _deathSound.Play();
            _sprite.Play("die");

        }
        if (_deathFrameCount > DeathFrames)
        {
            GD.Print("RESPAWNING!");
            _isRespawning = true;
            _deathFrameCount = 0;
            return;
        }
        _deathFrameCount++;
    }
    public void LevelUp()
    {
        _isLevelingUp = true;
    }
    private void StateTriggers()
    {
        if (!_isAlive)
        {
            _bufferVelocity = new Godot.Vector2(0,0);
        }
        if (_isRespawning)
        {
            _isAlive = true;
            _physicsState = PhysicsState.GENERAL_INTERACTION;
            _bufferPosition = new Godot.Vector2(0, GetViewportRect().Size.Y / 2);
            _sprite.Stop();
            _sprite.Play("jumping");
            _bufferVelocity.Y = JumpVelocity;
            _isRespawning = false;

        }
        if (_isLevelingUp)
        {
            _bufferPosition.Y = GetViewportRect().Size.Y / 2;
            _bufferVelocity.Y = JumpVelocity;
            _isLevelingUp = false;
        }
        if (_coyoteTime)
        {
            _coyoteFrameCount ++;
            if (_coyoteFrameCount > CoyoteFrames)
            {
                _coyoteFrameCount = 0;
                _coyoteTime = false;
            }
        }
        if (_jumpBuffer)
        {
            _jumpBufferCount++;
            if (_jumpBufferCount > JumpBufferFrames)
            {
                _jumpBufferCount = 0;
                _jumpBuffer = false;
            }
        }
    }
    private void DirectionalPhysics(ref double delta)
    {
        PollJump(ref delta);
        Godot.Vector2 direction = Input.GetVector("lucy_left", "lucy_right", "lucy_up", "lucy_down");
        if (_bufferVelocity.Y > 0)
        {
            _isJumping = false;
        }
        if (Math.Abs(direction.X) > 0.000001f)
        {
            _framesRunning++;
            if (direction.X != _previousXDirectionNormal)
            {
                _bufferVelocity.X = 0;
                _runningDirectionChanged = true;
            }
            if (Math.Abs(_bufferVelocity.X) < MaximumRunSpeed)
            {
                _bufferVelocity.X += direction.X * InstantVelocity * (float)delta * 60;
            }
            _isRunning = true;
        }
        else
        {
            _bufferVelocity.X = Mathf.MoveToward(Velocity.X, 0, 500);
            _isRunning = false;
            _framesRunning = 0;
        }
        if (direction.X > 0)
        {
            _sprite.FlipH = false;
        }
        else if (direction.X < 0)
        {
            _sprite.FlipH = true;
        }
        if (_isSuperJumping)
        {
            _sprite.Play("super_jump");
        }
        else if (_isJumping)
        {
            _sprite.Play("jumping");
        }
        else if (_bufferVelocity.Y >= 0)
        {
            if (Math.Abs(_bufferVelocity.X) > 50 || _framesRunning > 5 && HasPlatformInteraction())
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
        _previousXDirectionNormal = direction.X;
    }
}

