using Godot;
/// <summary>
/// 阳光
/// </summary>

public partial class Sun : Node2D
{
    private AnimatedSprite2D _sprite;

    [Export]
    public Vector2 _velocity = new Vector2(0, 0); // 阳光下落速度

    private GameManager _gameManager;

    public int suns = 0;
    private bool IsPointInsideSprite(Vector2 point)
    {
        if (_sprite == null || _sprite.SpriteFrames == null || string.IsNullOrEmpty(_sprite.Animation))
            return false;

        // 获取当前帧的纹理
        var tex = _sprite.SpriteFrames.GetFrameTexture(_sprite.Animation, _sprite.Frame);
        if (tex == null)
        {
            return false;
        }

        // 计算精灵的边界矩形
        Vector2 size = tex.GetSize() * _sprite.GlobalScale;
        Rect2 rect = new Rect2(_sprite.GlobalPosition - size * 0.5f, size);

        // 检查点是否在矩形内
        return rect.HasPoint(point);
    }

    /// <summary>
	/// 尝试找到场景中的GameManager节点
	/// </summary>
	private void FindGameManager()
    {
        // 查找场景中的游戏管理器
        var root = GetTree().Root;
        if (root != null)
        {
            _gameManager = root.GetNodeOrNull<GameManager>("GameManager");

            if (_gameManager == null)
            {
                // 尝试在当前场景中查找
                _gameManager = GetTree().CurrentScene?.GetNodeOrNull<GameManager>("GameManager");
            }

            if (_gameManager == null)
            {
                // 尝试在父节点中查找
                Node currentNode = this;
                while (currentNode != null && _gameManager == null)
                {
                    _gameManager = currentNode.GetNodeOrNull<GameManager>("GameManager");
                    currentNode = currentNode.GetParent();
                }
            }
        }

        if (_gameManager == null)
        {
            GD.PrintErr("GameManager not found in scene!");
        }
        else
        {
            GD.Print($"Found GameManager: {_gameManager.Name}");
        }
    }

    public override void _Ready()
    {
        FindGameManager();
        // 获取AnimatedSprite2D节点
        _sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if (_sprite == null)
        {
            GD.PrintErr($"Sun._Ready: No AnimatedSprite2D found in {Name}!");
        }
        _sprite.Animation = "default";
        _sprite.Play();
    }

    public override void _Process(double delta)
    {
        // 让阳光下落
        Position += _velocity * (float)delta;

        // 如果阳光超出屏幕底部，则移除它
        if (Position.Y > GetViewportRect().Size.Y)
        {
            QueueFree();
        }
    }

    private void OnChick()
    {

    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            int buttonIndex = (int)mouseEvent.ButtonIndex;

            if (mouseEvent.Pressed && buttonIndex == (int)MouseButton.Left)
            {
                // 检查是否点击了这个卡片
                Vector2 mousePos = GetGlobalMousePosition();

                // 只有鼠标在精灵上时才处理
                if (_sprite != null && IsPointInsideSprite(mousePos))
                {
                    _gameManager.AddSun(suns);
                    QueueFree();
                }
            }
        }
    }
}