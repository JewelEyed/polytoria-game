// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Attributes;
using Polytoria.Networking;
using Polytoria.Scripting;
using Polytoria.Shared;


namespace Polytoria.Datamodel;

[Instantiable]
public sealed partial class InteractionPrompt : Physical
{
	public const string PromptScenePath = "res://scenes/datamodel/InteractionPrompt.tscn";

	private Physical? _parent = null!;

	private bool _enabled = true;
	private float _maxDistance = 10.0f;
	private bool _requireFacing = true;
	private float _losThreshold = 0.5f;
	private float _activationTime = 0.5f;
	private string _title = "Interact";
	private string _subtitle = "Subtitle";




	private bool _useParentForVisibility = false;

	private Node3D _prompt = null!;
	private AnimationPlayer _animPlayer = null!;
	private TextureProgressBar _progressBar = null!;

	private bool _inRange = false;
	private bool _isMouseOverParent = false;
	private float _timeSpentActivating = 0.0f;


	[Editable, ScriptProperty, DefaultValue(true)]
	public bool Enabled
	{
		get => _enabled;
		set
		{
			_enabled = value;
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty, DefaultValue("Interact")]
	public string Title
	{
		get => _title;
		set
		{
			_title = value;
			_prompt.GetNode<Label>("SV/Control/Pivot/Text/Layout/Title").Text = _title;
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty, DefaultValue("Subtitle")]
	public string Subtitle
	{
		get => _subtitle;
		set
		{
			_subtitle = value;
			_prompt.GetNode<Label>("SV/Control/Pivot/Text/Layout/SubTitle").Text = _subtitle;
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty, DefaultValue(10.0f)]
	public float MaxDistance
	{
		get => _maxDistance;
		set
		{
			_maxDistance = value;
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty, DefaultValue(0.5f)]
	public float LineOfSightThreshold
	{
		get => _losThreshold;
		set
		{
			_losThreshold = value;
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty, DefaultValue(0.5f)]
	public float ActivationTime
	{
		get => _activationTime;
		set
		{
			_activationTime = value;
			OnPropertyChanged();
		}
	}

	[Editable, ScriptProperty, DefaultValue(false)]
	public bool UseParentForVisibility
	{
		get => _useParentForVisibility;
		set
		{
			_useParentForVisibility = value;
			OnPropertyChanged();
		}
	}



	[Editable, ScriptProperty, DefaultValue(true)]
	public bool RequireFacing
	{
		get => _requireFacing;
		set
		{
			_requireFacing = value;
			OnPropertyChanged();
		}
	}

	public bool CheckCanInteract()
	{
		if (_inRange)
		{
			if (_requireFacing)
			{
				return IsFacingPrompt();
			}
			else
			{
				return true;
			}
		}
		return false;
	}

	public bool IsFacingPrompt()
	{
		if (Root.Environment.CurrentCamera == null) {
			return false;
		}
		var playerTransform = Root.Environment.CurrentCamera.GetGlobalTransform(); 
		var targetF = -playerTransform.Basis.Z;
		var direction = (_prompt.GlobalTransform.Origin - playerTransform.Origin).Normalized();
		return targetF.Dot(direction) >= _losThreshold;
	}


	public void OnMouseEnterParent()
	{
		_isMouseOverParent = true;
	}

	public void OnMouseExitParent()
	{
		_isMouseOverParent = false;
	}

	public override void EnterTree()
	{
		if (Parent is Physical phy)
		{
			_parent = phy;
			phy.MouseEnter.Connect(OnMouseEnterParent);
			phy.MouseExit.Connect(OnMouseExitParent);
		}
		base.EnterTree();
	}

	public override void ExitTree()
	{
		_parent?.MouseEnter.Disconnect(OnMouseEnterParent);
		_parent?.MouseExit.Disconnect(OnMouseExitParent);
		_parent = null;
		base.ExitTree();
	}

	public override Node CreateGDNode() {
		_prompt = Globals.CreateInstanceFromScene<Node3D>(PromptScenePath);
		_animPlayer = _prompt.GetNode<AnimationPlayer>("AnimPlay");
		_progressBar = _prompt.GetNode<TextureProgressBar>("SV/Control/Pivot/Key/TextureProgressBar");
		return _prompt;
	}

	public override void Init()
	{
		base.Init();
		SetProcess(true);
	}


	public override void Process(double delta)
	{
		if (Root.SessionType != World.SessionTypeEnum.Client) { return; }
		if (!Root.IsLoaded) return;
		var distance = Root.Players.LocalPlayer?.GetGlobalPosition().DistanceTo(GetGlobalPosition());
		_inRange = distance <= _maxDistance;
		_prompt.Visible = false;
		if (_inRange && _enabled)
		{
			if (_useParentForVisibility)
			{
				if (_isMouseOverParent)
				{
					_prompt.Visible = true;
				}
			}
			else
			{
				_prompt.Visible = true;
			}
		}

		if (Input.IsActionPressed("interact"))
		{
			
			if (CheckCanInteract() && _enabled)
			{
				if (_timeSpentActivating == 0.0f) {
					_animPlayer.Play("InputStart");
				}
				_timeSpentActivating += (float)delta;
			}
			if (_timeSpentActivating >= _activationTime)
			{
				_timeSpentActivating = 0.0f;
				_animPlayer.Play("InputEnd");
				Interacted.Invoke(Root.Players.LocalPlayer);
				RpcId(1, nameof(TriggerInteracted));
			}
			
		}
		else
		{
			if (_timeSpentActivating > 0.0f) {
				_timeSpentActivating = 0.0f;
				_animPlayer.Play("InputEnd");
			}

		}
		_progressBar.Value = (_timeSpentActivating / _activationTime) * 100f;
		base.PhysicsProcess(delta);
	}
	
	[NetRpc(AuthorityMode.Any, TransferMode = TransferMode.Reliable)]
	private void TriggerInteracted() {
		Player? p = Root.Players.GetPlayerFromPeerID(RemoteSenderId);
		if (p == null) {
			return;
		}
		Interacted.Invoke(p);
	}

	[ScriptProperty] public PTSignal<Player> Interacted { get; private set; } = new();
}
