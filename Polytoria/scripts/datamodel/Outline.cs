using Godot;
using Polytoria.Attributes;
using Polytoria.Shared;

namespace Polytoria.Datamodel;

[Instantiable]
public partial class Outline : Dynamic
{

	private const string _shaderPath = "res://resources/shaders/outline/outline.gdshader";

	private bool _enabled = true;
	private float _outlineWidth = 2f;
	private Color _outlineColor = new(1f, 1f, 1f);

	private ShaderMaterial? _shader = null!;

	[Editable, ScriptProperty]
	public Color Color
	{
		get => _outlineColor;
		set
		{
			_outlineColor = value;
			_shader?.SetShaderParameter("color", new Vector4(_outlineColor.R, _outlineColor.G, _outlineColor.B, _outlineColor.A));
            EnsureOutlines(GDNode);
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public float OutlineWidth
	{
		get => _outlineWidth;
		set
		{
			_outlineWidth = value;
			_shader?.SetShaderParameter("outline_width", _outlineWidth);
            EnsureOutlines(GDNode);
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty]
	public bool Enabled
	{
		get => _enabled;
		set
		{
			_enabled = value;
			_shader?.SetShaderParameter("enabled", _enabled);
            EnsureOutlines(GDNode);
			OnPropertyChanged();
		}
	}

	public override void Init()
	{
		base.Init();
        PT.Print("hai im snappled");
	    _shader = new ShaderMaterial();
        _shader.Shader = (Shader)ResourceLoader.Load(_shaderPath);	}

	public void EnsureOutlines(Node node)
	{
        PT.Print(node.Name);
		if (_shader == null) {
            return;
        }

		foreach (var child in node.GetChildren(true))
		{
			if (child is MeshInstance3D)
			{
                PT.Print(child.Name);
				var meshInstance = (MeshInstance3D)child;
				meshInstance.MaterialOverlay = _shader;
			}
            var childrenOfChild = child.GetChildren();
			if (childrenOfChild.Count > 0)
			{
				foreach (var childOfChild in childrenOfChild)
				{
					EnsureOutlines(childOfChild);
				}
			}
		}
	}

	public override void EnterTree()
	{
		base.EnterTree();
		EnsureOutlines(GDNode);
	}

	public override void ExitTree()
	{
		base.ExitTree();
		_shader?.SetShaderParameter("enabled", false);
	}

}