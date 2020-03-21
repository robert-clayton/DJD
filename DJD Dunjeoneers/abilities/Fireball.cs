using Godot;
using System;

public class Fireball : AbilityBase
{
    private bool exploded = false;
    private Particles2D explosionEmitter = new Particles2D();
    private Particles2D smokeTrailEmitter = new Particles2D();
    private Particles2D fireTrailEmitter = new Particles2D();
    private ParticlesMaterial explosionMaterial = ResourceLoader.Load("res://particles/explosion.tres").Duplicate() as ParticlesMaterial;
    private ParticlesMaterial smokeTrailMaterial = ResourceLoader.Load("res://particles/smoke_trail.tres") as ParticlesMaterial;
    private ParticlesMaterial fireTrailMaterial = ResourceLoader.Load("res://particles/fire_trail.tres") as ParticlesMaterial;
    private Sprite sprite = new Sprite();
    private Texture spriteImage = ResourceLoader.Load("res://assets/sprites/fireball.png") as Texture;
    
    protected Light2D light = new Light2D();
    private Area2D hitArea = new Area2D();
    public Tween onHitTween = new Tween();
    private Timer timeLimit = new Timer();

    public Fireball() : base(){}

    public Fireball(Vector2 _direction, Vector2 _pos, int _targetLayer) : base(_direction, _pos, _targetLayer){
        damage = 100f;
        knockbackStrength = 50f;
        maxVelocity = 150;
        acceleration = 200;
        effectRadius = 30f;

        Texture gradient = ResourceLoader.Load("res://assets/gradients/radial.png") as Texture;
        light.Texture = gradient;
        light.Scale = new Vector2(.1f, .1f);
        light.Color = new Color(1f, .7f, .7f, .2f);
        AddChild(light);

        VisibilityNotifier2D onScreenNotifier = new VisibilityNotifier2D();
        onScreenNotifier.Connect("screen_exited", this, "queue_free");
        AddChild(onScreenNotifier);

        Position = Position + direction * 10f;

        // Sprite
        sprite.Texture = spriteImage;
        AddChild(sprite);

        // Collider
        hitArea.SetCollisionMaskBit(targetLayer, true);
        AddChild(hitArea);
        CollisionShape2D shape = new CollisionShape2D();
        shape.Transform = new Godot.Transform2D(0f, new Vector2(-1, 0f));
        CircleShape2D circle = new CircleShape2D();
        circle.Radius = 6;
        shape.Shape = circle;
        hitArea.AddChild(shape);
        hitArea.Connect("area_entered", this, "_OnHurtAreaEnter");

        // Emitters
        explosionEmitter.ProcessMaterial = explosionMaterial;
        explosionEmitter.Emitting = false;
        explosionEmitter.Amount = 150;
        explosionEmitter.Lifetime = 4;
        explosionEmitter.OneShot = true;
        explosionEmitter.SpeedScale = 2;
        explosionEmitter.Explosiveness = 1;
        explosionEmitter.ZIndex = -100;
        AddChild(explosionEmitter);
        
        smokeTrailEmitter.Amount = 200;
        smokeTrailEmitter.Preprocess = 0.5f;
        smokeTrailEmitter.ProcessMaterial = smokeTrailMaterial;
        AddChild(smokeTrailEmitter);

        fireTrailEmitter.Amount = 100;
        fireTrailEmitter.Preprocess = 0.5f;
        fireTrailEmitter.SpeedScale = 1.5f;
        fireTrailEmitter.Explosiveness = .1f;
        fireTrailEmitter.Randomness = 1;
        fireTrailEmitter.ProcessMaterial = fireTrailMaterial;
        fireTrailEmitter.Transform = new Godot.Transform2D(0f, new Vector2(-1, 0f));
        AddChild(fireTrailEmitter);

        // Timers
        var targetColor =  new Color(light.Color.r, light.Color.g, light.Color.b, light.Color.a*2);
        var targetColorNone = new Color(light.Color.r, light.Color.g, light.Color.b, 0);
        var targetScale = light.Scale * 3 * Mathf.Clamp((float)new Random().NextDouble(), .5f, 1f);
        onHitTween.InterpolateProperty(light, "color", light.Color, targetColor, .2f, easeType: Tween.EaseType.Out);
        onHitTween.InterpolateProperty(light, "color", targetColor, targetColorNone, .1f, easeType: Tween.EaseType.Out, delay: .2f);
        onHitTween.InterpolateProperty(light, "scale", light.Scale, targetScale, .1f);
        onHitTween.InterpolateProperty(explosionMaterial, "gravity", explosionMaterial.Gravity, default(Vector3), 0.1f, delay: .2f);
        onHitTween.InterpolateCallback(this, 1f,"queue_free");
        AddChild(onHitTween);

        timeLimit.WaitTime = 1f;
        timeLimit.OneShot = true;
        timeLimit.Connect("timeout", this, nameof(Explode));
        AddChild(timeLimit);
    }

    public override void _Ready(){
        Rotate(GetAngleTo(Position + direction));
        timeLimit.Start();
    }

    public override void _PhysicsProcess(float delta){
        if (!exploded){
            velocity = Mathf.Min(velocity + acceleration * delta, maxVelocity);
            Translate(direction * velocity * delta);
            ZIndex = Mathf.FloorToInt(Position.y);
        }
    }

    public virtual void _OnHurtAreaEnter(Area2D _ = null){
        Explode();
    }

    public virtual void Explode(){
        onHitTween.Start();
        fireTrailEmitter.Emitting = false;
        smokeTrailEmitter.Emitting = false;
        smokeTrailEmitter.Scale = default(Vector2);
        explosionMaterial.Gravity = new Vector3(velocity/2, 0f, 0f);
        explosionEmitter.Emitting = true;
        exploded = true;
        sprite.Visible = false;
        hitArea.CollisionLayer = 0;
        hitArea.CollisionMask = 0;
        
        // Affect target(s)
        foreach (Enemy target in GetTree().GetNodesInGroup("Enemies")){
            if (Position.DistanceTo(target.Position) < effectRadius)
                target.Damage(damage, GlobalPosition.DirectionTo(target.GlobalPosition) * knockbackStrength);
        }
    }
}
