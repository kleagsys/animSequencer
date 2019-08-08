/*
Create by Kleagsys
Version:  1.1
	
Instructions:
	Add to any object in the scene.
	Use the Select pull-down menus to create the order your animation will play.
	Animation 0 plays first then Animation 1 and so on.
	Once the order is established increment the Animation Loops slider to set the number of time you want each individual animation to loop (It rounds to whole integers)
	Animation 0 pull-down on left is tied to Animation 0 Loops slider on right, each animation has their own pair
	
	Once everything is set to your liking Click Reset Animation button to sync model with starting animation
	Then Press Start
	
	SwitchTime Modifier is a threshold setting used to alter the time when the plugin switches to the next animations
	

*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public class animSequencer : MVRScript {

	// Name of plugin
	public static string pluginName = "Animation Sequencer";
	// InGame UI Elements
	private UIDynamicButton _startButton;
	private UIDynamicButton _stopButton;
	private UIDynamicButton _resetButton;
	private JSONStorableBool _runOnStartup;
	private UIDynamicButton _syncStartButton;
	private UIDynamicButton _syncEndButton;
	private JSONStorableFloat _animModifier;
	private JSONStorableFloat _animRandomizer;
	
	private bool _switch = true;
	private int _loopCount = 1;
	private int _animSequence = 0;
	private float _animTotal = 1;

	private List<string> _animList = new List<string> ();
	private List<Atom> _animAtoms = new List<Atom> ();
	private List<AnimationPattern> _animationPattern = new List<AnimationPattern> ();
	private List<JSONStorableFloat> _animLoops = new List<JSONStorableFloat> ();
	private List<JSONStorableStringChooser> _animChooser = new List<JSONStorableStringChooser> ();
	private List<UIDynamicPopup> _animChooserPopup = new List<UIDynamicPopup> ();
	
	private Atom _atomUpdate;
	private AnimationPattern _animUpdate;

	private float _animTotalTime;
	private float _animCurrentTime;
	private System.Random _rng = new System.Random();
	private float _randomNum;

	// Function to initiate plugin
	public override void Init () {
		try {
			pluginLabelJSON.val = "Animation Sequencer";
			
			_startButton = CreateButton ("Start Animations", false);
			if (_startButton != null) {
				_startButton.button.onClick.AddListener (startButtonCallback);
			}
			_stopButton = CreateButton ("Stop Animations", false);
			if (_stopButton != null) {
				_stopButton.button.onClick.AddListener (stopButtonCallback);
			}
			_resetButton = CreateButton ("Reset Animations", false);
			if (_resetButton != null) {
				_resetButton.button.onClick.AddListener (resetButtonCallback);
			}
			_runOnStartup = new JSONStorableBool("Run on Startup", false, runOnStartUpCallback);
			RegisterBool(_runOnStartup);
            CreateToggle(_runOnStartup, true);
			
			_syncStartButton = CreateButton ("Reserved for Future Feature", true);
			if (_syncStartButton != null) {
				// Method to align all start steps
			}
			
			_syncEndButton = CreateButton ("Reserved for Future Feature", true);
			if (_syncEndButton != null) {
				// Method to align end step with next start step
			}

			// Modifier used to adjust the animation switching threshold
			_animModifier = new JSONStorableFloat ("SwitchTime Modifier", 0.05f, 0f, 1f, true, true);
			RegisterFloat (_animModifier);
			CreateSlider (_animModifier, false);
			
			// Add Random Loop Count
            _animRandomizer = new JSONStorableFloat("Loop Count Randomizer", 0.0f, 0f, 100f, true, true);
            RegisterFloat(_animRandomizer);
			CreateSlider(_animRandomizer, true);
			
			// Get a list of all Animations
			_animationPattern = SuperController.singleton.GetAllAnimationPatterns ();
			// Create List of Animations for the Dropdown Menu
			for (var i = 0; i < _animationPattern.Count; i++) {
				_animList.Add (_animationPattern[i].uid.ToString ());
			}
			CreateSpacer(false).height = 12;
			// Setup Pull Down Interface for Each Animation
			for (var i = 0; i < _animationPattern.Count; i++) {
				_animAtoms.Add (SuperController.singleton.GetAtomByUid (_animationPattern[i].uid.ToString ()));
				_animChooser.Add (new JSONStorableStringChooser ("Animation Chooser", _animList, _animationPattern[i].uid.ToString (), "Select Animation " + i.ToString (), AnimChooserCallback));
				_animChooserPopup.Add (CreatePopup (_animChooser[i], false));
				_animChooserPopup[i].labelWidth = 200f;
				CreateSpacer(false).height = 24;
				_animLoops.Add (new JSONStorableFloat ("Animation " + i.ToString () + " Loops", 1f, 1f, 100f, true, true));
				RegisterFloat (_animLoops[i]);
				CreateSlider (_animLoops[i], true);
			}
			// Turn off all Animation to Initialize Scene
			for (var i = 0; i < _animationPattern.Count; i++) {
				if (_animAtoms[i].on) _animAtoms[i].ToggleOn ();
			}
			_animTotal = _animList.Count;
			_animTotalTime = _animationPattern[_animSequence].GetTotalTime ();
			_randomNum = _rng.Next((int)Math.Round(_animRandomizer.val));

		} catch (Exception e) {
			SuperController.LogError ("Exception caught: " + e);
		}
	}
	// Update is called with each rendered frame by Unity
	void Update () {
		try {
			_animCurrentTime = _animationPattern[_animSequence].GetCurrentTimeCounter ();
			if ((_animCurrentTime > (Time.deltaTime + _animModifier.val)) && (_animCurrentTime < (_animTotalTime - Time.deltaTime - _animModifier.val))) {
				_switch = true;
			}
			if ((_animCurrentTime > (_animTotalTime - Time.deltaTime - _animModifier.val)) && _switch) {
				if (_loopCount >= _animLoops[_animSequence].val + _randomNum) {
					// move onto next animation sequence
					if (_animAtoms[_animSequence].on) _animAtoms[_animSequence].ToggleOn (); // Turn OFF current Animation
					_animationPattern[_animSequence].ResetAnimation ();
					_animSequence++;
					if (_animSequence >= _animTotal) {
						_animSequence = 0;
					}
					_animationPattern[_animSequence].ResetAnimation ();
					if (!_animAtoms[_animSequence].on) _animAtoms[_animSequence].ToggleOn (); // Turn ON Next Animation in the Sequence
					_animTotalTime = _animationPattern[_animSequence].GetTotalTime ();
					_loopCount = 1;
					_switch = false;
					_randomNum = _rng.Next((int)Math.Round(_animRandomizer.val));
				} else {
					_loopCount++;
					_switch = false;
				}
			}
		} catch (Exception e) {
			SuperController.LogError ("Exception caught: " + e);
		}
	}

	private void runOnStartUpCallback (bool b) {
		if (b)
		{
			for (var i = 0; i < _animationPattern.Count; i++) 
			{
				if (_animAtoms[i].on) _animAtoms[i].ToggleOn ();
				_animationPattern[i].Play();
			}
			_animationPattern[0].ResetAnimation ();
			if (!_animAtoms[0].on) _animAtoms[0].ToggleOn ();
		}
	}

	private void AnimChooserCallback (string s) {
		for (var i = 0; i < _animTotal; i++) {
			_atomUpdate = SuperController.singleton.GetAtomByUid (_animChooser[i].val);
			_animUpdate = _atomUpdate.GetStorableByID ("AnimationPattern") as AnimationPattern;
			_animAtoms[i] = _atomUpdate;
			_animationPattern[i] = _animUpdate;
		}
	}

	private void startButtonCallback () {
		_animTotalTime = _animationPattern[0].GetTotalTime ();
		_loopCount = 1;
		_animSequence = 0;
		_switch = false;
		_animationPattern[0].ResetAnimation ();
		if (!_animAtoms[0].on) _animAtoms[0].ToggleOn ();
		_animationPattern[0].Play ();
	}

	private void stopButtonCallback () {
		for (var i = 0; i < _animationPattern.Count; i++) {
			if (_animAtoms[i].on) _animAtoms[i].ToggleOn ();
			_animationPattern[i].Play();
		}
	}

	private void resetButtonCallback () {
		for (var i = 0; i < _animationPattern.Count; i++) {
			if (_animAtoms[i].on) _animAtoms[i].ToggleOn ();
			_animationPattern[i].ResetAnimation();
			_animationPattern[i].Play();
		}
		_animationPattern[0].Pause();
		if (!_animAtoms[0].on) _animAtoms[0].ToggleOn();
	}
}