/*
Create by Paradym
	
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
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System;
using System.IO.Ports; 

namespace SerialCtrl {
    public class SerialTrial : MVRScript {
     
        // Name of plugin
        public static string pluginName = "Animation Sequencer";
		// InGame UI Elements
		protected UIDynamicButton startButton;
		protected UIDynamicButton stopButton;
		protected UIDynamicButton resetButton;
		protected JSONStorableFloat _animModifier;
		
		private bool _switch = true;
		private int _loopCount = 1;
        private int _animSequence = 0;
		private float _animTotal = 1;
		
		List<string> _animList = new List<string>();
        List<Atom> _animAtoms = new List<Atom>();	
		List<AnimationPattern> _animationPattern = new List<AnimationPattern>();
		List<JSONStorableFloat> _animLoops = new List<JSONStorableFloat>();
		List<JSONStorableStringChooser> _animChooser = new List<JSONStorableStringChooser>();
		List<UIDynamicPopup> _animChooserPopup = new List<UIDynamicPopup>();
		
		private Atom _atomUpdate;
		private AnimationPattern _animUpdate;
		
		private float _animTotalTime;
		private float _animCurrentTime;
		
        // Function to initiate plugin
        public override void Init() 
		{
            try 
			{
				pluginLabelJSON.val = "Animation Sequencer";
				startButton = CreateButton("Start Animations", false);
                if (startButton != null) {
                    startButton.button.onClick.AddListener(StartButtonCallback);
                }
				stopButton = CreateButton("Stop Animations", false);
                if (stopButton != null) {
                    stopButton.button.onClick.AddListener(StopButtonCallback);
                }
				resetButton = CreateButton("Reset Animations", false);
                if (resetButton != null) {
                    resetButton.button.onClick.AddListener(ResetButtonCallback);
                }
				// Modifier used to adjust the animation switching threshold
                _animModifier = new JSONStorableFloat("SwitchTime Modifier", 0.05f, 0f, 1f, true, true);
                RegisterFloat(_animModifier);
                CreateSlider(_animModifier, true);
				// Get a list of all Animations
				_animationPattern = SuperController.singleton.GetAllAnimationPatterns();
				// Create List of Animations for the Dropdown Menu
				for(var i=0; i < _animationPattern.Count; i++)
				{
					_animList.Add(_animationPattern[i].uid.ToString());
				}
				// Setup Pull Down Interface for Each Animation
				for(var i=0; i < _animationPattern.Count; i++)
				{
					_animAtoms.Add(SuperController.singleton.GetAtomByUid(_animationPattern[i].uid.ToString()));
					_animChooser.Add(new JSONStorableStringChooser("Animation Chooser", _animList, _animationPattern[i].uid.ToString(), "Select Animation " + i.ToString(), AnimChooserCallback));
					_animChooserPopup.Add(CreatePopup(_animChooser[i], false));
					_animChooserPopup[i].labelWidth = 200f;
					_animLoops.Add(new JSONStorableFloat("Animation " + i.ToString() + " Loops", 1f, 1f, 100f, true, true));
					RegisterFloat(_animLoops[i]);
					CreateSlider(_animLoops[i], true);
				}
				// Turn off all Animation to Initialize Scene
				for(var i=0; i < _animationPattern.Count; i++)
				{
					if (_animAtoms[i].on) _animAtoms[i].ToggleOn();
				}

				//if (!_animAtoms[0].on) _animAtoms[0].ToggleOn();  // <--Uncomment this line if you want the first animation to start on scene load

				_animTotal = _animList.Count;
				_animTotalTime = _animationPattern[_animSequence].GetTotalTime();
				
            } catch (Exception e) {
                SuperController.LogError("Exception caught: " + e);
            }
        }
        // Update is called with each rendered frame by Unity
        void Update() 
		{
            try 
			{
					_animCurrentTime = _animationPattern[_animSequence].GetCurrentTimeCounter();
					
					if ((_animCurrentTime > (Time.deltaTime + _animModifier.val)) && (_animCurrentTime < (_animTotalTime - Time.deltaTime - _animModifier.val)))
					{	
						_switch = true;
					}
					if ((_animCurrentTime > (_animTotalTime - Time.deltaTime - _animModifier.val)) && _switch)
					{
						if (_loopCount >= _animLoops[_animSequence].val)
						{
							// move onto next animation sequence
							if (_animAtoms[_animSequence].on) _animAtoms[_animSequence].ToggleOn();  // Turn OFF current Animation
							_animationPattern[_animSequence].ResetAnimation();
							_animSequence++;
							if (_animSequence >= _animTotal)
							{
								_animSequence = 0;
							}
							_animationPattern[_animSequence].ResetAnimation();
							if (!_animAtoms[_animSequence].on) _animAtoms[_animSequence].ToggleOn();  // Turn ON Next Animation in the Sequence
							_animTotalTime = _animationPattern[_animSequence].GetTotalTime();
							_loopCount = 1;
							_switch = false;
						}
						else
						{
							_loopCount++;
							_switch = false;
							
						} 
					}
            } 
			catch (Exception e) 
			{
                SuperController.LogError("Exception caught: " + e);
            }
        }

		protected void AnimChooserCallback(string s)
		{

			for(var i=0; i < _animTotal; i++)
			{
				_atomUpdate = SuperController.singleton.GetAtomByUid(_animChooser[i].val);				
				_animUpdate = _atomUpdate.GetStorableByID("AnimationPattern") as AnimationPattern;
				_animAtoms[i] = _atomUpdate;
				_animationPattern[i] = _animUpdate;
			}
        }
		
        protected void StartButtonCallback()
		{
			_animTotalTime = _animationPattern[0].GetTotalTime();
			_loopCount = 1;
			_animSequence = 0;
			_switch = false;
			_animationPattern[0].ResetAnimation();
			if (!_animAtoms[0].on) _animAtoms[0].ToggleOn();
			_animationPattern[0].Play();
        }
		
        protected void StopButtonCallback()
		{
			for(var i=0; i < _animationPattern.Count; i++)
			{
				//_animationPattern[i].Pause();
				if (_animAtoms[i].on) _animAtoms[i].ToggleOn();
			}
        }		
		
		protected void ResetButtonCallback()
		{
			for(var i=0; i < _animationPattern.Count; i++)
			{
				//_animationPattern[i].Pause();
				if (_animAtoms[i].on) _animAtoms[i].ToggleOn();
				_animationPattern[i].ResetAndPlay();
			}
			_animationPattern[0].Pause();
			if (!_animAtoms[0].on) _animAtoms[0].ToggleOn();
        }
    }
}