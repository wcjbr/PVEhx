using Godot;
using System;

/// <summary>
/// 简化的发射类植物
/// </summary>
public partial class ShootingPlant : Plant
{
	private bool _isBullet = false;
	private float _fireInterval = 0.5f;
	private float _fireTimer = 0;
	private float _bulletSpeed = 400f;

	public override void _Ready()
	{
		base._Ready();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		
		if (_isBullet)
		{
			Position += new Vector2(_bulletSpeed, 0) * (float)delta;
		}
		else if (_isPlanted)
		{
			_fireTimer += (float)delta;
			if (_fireTimer >= _fireInterval)
			{
				_fireTimer = 0;
				FireBullet();
			}
		}
	}
	
	private void FireBullet()
	{
		var bullet = Duplicate() as ShootingPlant;
		if (bullet != null)
		{
			GetParent().AddChild(bullet);
			bullet.GlobalPosition = GlobalPosition;
			bullet._isBullet = true;
			bullet._isPlanted = false;
			
			if (bullet._sprite != null && bullet._sprite.SpriteFrames != null && 
				bullet._sprite.SpriteFrames.HasAnimation("Bullet"))
			{
				bullet._sprite.Animation = "Bullet";
				bullet._sprite.Play();
			}
		}
	}
	
	protected override void UpdateAnimation()
	{
		if (_sprite != null && _sprite.SpriteFrames != null)
		{
			if (_isBullet && _sprite.SpriteFrames.HasAnimation("Bullet"))
			{
				if (_sprite.Animation != "Bullet")
				{
					_sprite.Animation = "Bullet";
					_sprite.Play();
				}
				return;
			}
		}
		base.UpdateAnimation();
	}
}
