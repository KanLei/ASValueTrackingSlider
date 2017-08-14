
using System;
using System.Linq;
using UIKit;
using Foundation;
using CoreGraphics;
using System.Collections.Generic;

namespace ASValueTrackingSlider.ValueTrackingSlider
{
	public interface IASValueTrackingSliderDataSource
	{
		string Slider(ValueTrackingSlider slider, float value);
	}

	public interface IASValueTrackingSliderDelegate
	{
		void SliderWillDisplayPopUpView(ValueTrackingSlider slider);
		void SliderWillHidePopUpView(ValueTrackingSlider slider);
		void SliderDidHidePopUpView(ValueTrackingSlider slider);
	}

	[Register(nameof(ValueTrackingSlider))]
	public class ValueTrackingSlider : UISlider, IASValuePopUpViewDelegate
	{
		public IASValueTrackingSliderDataSource DataSource { get; set; }
		public IASValueTrackingSliderDelegate Delegate { get; set; }

		private ValuePopUpView PopUpView { get; set; }
		private bool PopUpViewAlwaysOn { get; set; }

		private NSNumber[] keyTimes;
		private nfloat valueRange;

		public ValueTrackingSlider(CGRect frame) : base(frame)
		{
			Setup();
		}

		[Export("initWithCoder:")]
		public ValueTrackingSlider(NSCoder coder) : base(coder)
		{
			Setup();
		}

		public ValueTrackingSlider(IntPtr handle) : base(handle)
		{

		}

		private bool autoAdjustTrackColor;
		public bool AutoAdjustTrackColor
		{
			get
			{
				return autoAdjustTrackColor;
			}
			set
			{
				if (autoAdjustTrackColor == value) return;

				autoAdjustTrackColor = value;

				// setMinimumTrackTintColor has been overridden to also set autoAdjustTrackColor to NO
				// therefore super's implementation must be called to set minimumTrackTintColor
				if (!value)
				{
					base.MinimumTrackTintColor = null;  // sets track to default blue color
				}
				else
				{
					base.MinimumTrackTintColor = PopUpView.OpaqueColor;
				}
			}
		}

		private UIColor textColor;
		public UIColor TextColor
		{
			get { return textColor; }
			set
			{
				textColor = value;
				PopUpView.SetTextColor(value);
			}
		}

		private UIFont font;
		public UIFont Font
		{
			get { return font; }
			set
			{
				font = value;
				PopUpView.SetFont(font);
			}
		}

		// setting the value of 'popUpViewColor' overrides 'popUpViewAnimatedColors' and vice versa
		// the return value of 'popUpViewColor' is the currently displayed value
		// this will vary if 'popUpViewAnimatedColors' is set (see below)
		private UIColor popUpViewColor;
		public UIColor PopUpViewColor
		{
			get
			{
				return PopUpView.Color ?? popUpViewColor;
			}
			set
			{
				popUpViewColor = value;
				popUpViewAnimatedColors = null; // animated colors should be discarded
				PopUpView.Color = value;

				if (AutoAdjustTrackColor)
				{
					base.MinimumTrackTintColor = PopUpView.OpaqueColor;
				}
			}
		}

		// pass an array of 2 or more UIColors to animate the color change as the slider moves
		private UIColor[] popUpViewAnimatedColors;
		public UIColor[] PopUpViewAnimatedColors
		{
			get { return popUpViewAnimatedColors; }
			set
			{
				popUpViewAnimatedColors = value;
				SetPopUpViewAnimatedColors(value, null);
			}
		}


		// if 2 or more colors are present, set animated colors
		// if only 1 color is present then call 'setPopUpViewColor:'
		// if arg is nil then restore previous _popUpViewColor
		public void SetPopUpViewAnimatedColors(UIColor[] colors, NSNumber[] positions)
		{
			if (positions != null && positions.Length != colors.Length)
			{
				throw new ArgumentException("PopUpViewAnimatedColors and locations should contain the same number of items.");
			}

			popUpViewAnimatedColors = colors;
			keyTimes = KeyTimesFromSliderPositions(positions);

			if (colors.Length >= 2)
			{
				PopUpView.SetAnimatedColors(colors, keyTimes);
			}
			else
			{
				PopUpViewColor = colors.Last() ?? popUpViewColor;
			}
		}


		public nfloat PopUpViewCornerRadius
		{
			get { return PopUpView.CornerRadius; }
			set
			{
				PopUpView.CornerRadius = value;
			}
		}


		public nfloat PopUpViewArrowLength
		{
			get { return PopUpView.ArrowLength; }
			set
			{
				PopUpView.ArrowLength = value;
			}
		}

		// width padding factor of the popUpView, default is 1.15
		public nfloat PopUpViewWidthPaddingFactor
		{
			get
			{
				return PopUpView.WidthPaddingFactor;
			}
			set
			{
				PopUpView.WidthPaddingFactor = value;
			}
		}

		// height padding factor of the popUpView, default is 1.1
		public nfloat PopUpViewHeightPaddingFactor
		{
			get { return PopUpView.HeightPaddingFactor; }
			set
			{
				PopUpView.HeightPaddingFactor = value;
			}
		}


		private void Setup()
		{
			autoAdjustTrackColor = true;
			valueRange = MaxValue - MinValue;
			PopUpViewAlwaysOn = false;

			var formatter = new NSNumberFormatter();
			formatter.NumberStyle = NSNumberFormatterStyle.Decimal;
			formatter.RoundingMode = NSNumberFormatterRoundingMode.HalfUp;
			formatter.MaximumFractionDigits = 2;
			formatter.MinimumFractionDigits = 2;
			numberFormatter = formatter;

			PopUpView = new ValuePopUpView(CGRect.Empty);
			popUpViewColor = UIColor.FromHSBA(0.6f, 0.6f, 0.5f, 0.8f);

			PopUpView.Alpha = 0.0f;
			PopUpView.Delegate = this;
			this.AddSubview(PopUpView);

			TextColor = UIColor.White;
			Font = UIFont.BoldSystemFontOfSize(22);
		}


		public override float MaxValue
		{
			get
			{
				return base.MaxValue;
			}
			set
			{
				base.MaxValue = value;
				valueRange = MaxValue - MinValue;
			}
		}

		public override float MinValue
		{
			get
			{
				return base.MinValue;
			}
			set
			{
				base.MinValue = value;
				valueRange = MaxValue - MinValue;
			}
		}

		public void SetMaxFractionDigitsDisplayed(nint maxDigits)
		{
			numberFormatter.MaximumFractionDigits = maxDigits;
			numberFormatter.MinimumFractionDigits = maxDigits;
		}

		private NSNumberFormatter numberFormatter;
		public NSNumberFormatter NumberFormatter
		{
			get { return numberFormatter.Copy() as NSNumberFormatter; }
			set
			{
				numberFormatter = value.Copy() as NSNumberFormatter;
			}
		}


		// Present the popUpView manually, without touch event.
		public void ShowPopUpView(bool animated)
		{
			PopUpViewAlwaysOn = true;
			ShowPopUpViewAnimated(animated);
		}

		// The popUpView will not hide again until you call 'HidePopUpView'
		public void HidePopUpView(bool animated)
		{
			PopUpViewAlwaysOn = false;
			HidePopUpViewAnimated(animated);
		}


		/// IASValuePopUpViewDelegate

		public void ColorDidUpdate(UIColor opaqueColor)
		{
			base.MinimumTrackTintColor = opaqueColor;
		}

		public nfloat CurrentValueOffset => (Value - MinValue) / valueRange;


        private void UpdatePopUpView()
		{
			CGSize popUpViewSize;
			string valueString = DataSource?.Slider(this, this.Value) ?? "";
			if (!string.IsNullOrWhiteSpace(valueString))
			{
				popUpViewSize = PopUpView.PopUpSizeForString(valueString);
			}
			else
			{
				valueString = numberFormatter.StringFromNumber(this.Value);
				popUpViewSize = CalculatePopUpViewSize();
			}

			// calculate the popUpView frame
			CGRect thumbRect = ThumbRect();
			nfloat thumbW = thumbRect.Size.Width;
			nfloat thumbH = thumbRect.Size.Height;

			CGRect popUpRect = thumbRect.Inset((thumbW - popUpViewSize.Width) / 2, (thumbH - popUpViewSize.Height) / 2);
			popUpRect.Y = thumbRect.Y - popUpViewSize.Height;

			// determine if popUpRect extends beyond the frame of the progress view
			// if so adjust frame and set the center offset of the PopUpView's arrow
			nfloat minOffsetX = popUpRect.GetMinX();
			nfloat maxOffsetX = popUpRect.GetMaxX() - this.Bounds.Width;

			nfloat offset = minOffsetX < 0.0f ? minOffsetX : (maxOffsetX > 0.0f ? maxOffsetX : 0.0f);
			popUpRect.X -= offset;

			PopUpView.SetFrame(popUpRect, offset, valueString);
		}

        private CGSize CalculatePopUpViewSize()
		{
			// negative values need more width than positive values
			var minStr = new NSString(NumberFormatter.StringFromNumber(NSNumber.FromFloat(MinValue)));
			CGSize minValSize = PopUpView.PopUpSizeForString(minStr);
			var maxStr = new NSString(NumberFormatter.StringFromNumber(NSNumber.FromFloat(MaxValue)));
			CGSize maxValSize = PopUpView.PopUpSizeForString(maxStr);

			return minValSize.Width >= maxValSize.Width ? minValSize : maxValSize;
		}

		// takes an array of NSNumbers in the range self.minimumValue - self.maximumValue
		// returns an array of NSNumbers in the range 0.0 - 1.0
        private NSNumber[] KeyTimesFromSliderPositions(NSNumber[] positions)
		{
			if (positions == null) return null;

			Array.Sort(positions);

			var temp = new List<NSNumber>();
			foreach (var num in positions)
			{
				temp.Add(NSNumber.FromNFloat((num.FloatValue - MinValue) / valueRange));
			}
			return temp.ToArray();
		}

        private CGRect ThumbRect()
		{
			return ThumbRectForBounds(this.Bounds, this.TrackRectForBounds(this.Bounds), this.Value);
		}

        private void ShowPopUpViewAnimated(bool animated)
		{
			Delegate?.SliderWillDisplayPopUpView(this);
			PopUpView.ShowAnimated(animated);
		}

        private void HidePopUpViewAnimated(bool animated)
		{
			Delegate?.SliderWillHidePopUpView(this);
			PopUpView.HideAnimated(animated, () =>
			{
				Delegate?.SliderDidHidePopUpView(this);
			});
		}


		/// override

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			UpdatePopUpView();
		}

		public override void MovedToWindow()
		{
			base.MovedToWindow();

			if (this.Window == null)  // removed from window - cancel notifications
			{
				NSNotificationCenter.DefaultCenter.RemoveObserver(this);
			}
			else  // added to window - register notifications
			{
				if (PopUpViewAnimatedColors != null)  // restart color animation if needed
				{
					PopUpView.SetAnimatedColors(popUpViewAnimatedColors, keyTimes);
				}

				NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidBecomeActiveNotification, (obj) =>
				{
					// ensure animation restarts if app is closed then becomes active again
					if (popUpViewAnimatedColors != null)
					{
						PopUpView.SetAnimatedColors(popUpViewAnimatedColors, keyTimes);
					}
				});
			}

		}

		public override float Value
		{
			get
			{
				return base.Value;
			}
			set
			{
				base.Value = value;

				PopUpView.SetAnimationOffset(CurrentValueOffset, (opaqueReturnColor) =>
				{
					base.MinimumTrackTintColor = opaqueReturnColor;
				});
			}
		}

		public override void SetValue(float value, bool animated)
		{
			if (animated)
			{
				PopUpView.AnimateBlock((duration) =>
				{
					UIView.Animate(duration, () =>
					{
						base.SetValue(value, animated);
						PopUpView.SetAnimationOffset(CurrentValueOffset, (opaqueReturnColor) =>
						{
							base.MinimumTrackTintColor = opaqueReturnColor;
						});
						LayoutIfNeeded();
					});
				});
			}
			else
			{
				base.SetValue(value, animated);
			}
		}

		public override UIColor MinimumTrackTintColor
		{
			get
			{
				return base.MinimumTrackTintColor;
			}
			set
			{
				AutoAdjustTrackColor = false;  // if a custom value is set then prevent auto coloring
				base.MinimumTrackTintColor = value;
			}
		}


		public override bool BeginTracking(UITouch uitouch, UIEvent uievent)
		{
			bool begin = base.BeginTracking(uitouch, uievent);
			if (begin && !PopUpViewAlwaysOn)
			{
				ShowPopUpViewAnimated(true);
			}
			return begin;
		}

		public override bool ContinueTracking(UITouch uitouch, UIEvent uievent)
		{
			bool continueTrack = base.ContinueTracking(uitouch, uievent);
			if (continueTrack)
			{
				PopUpView.SetAnimationOffset(CurrentValueOffset, (opaqueReturnColor) =>
				{
					base.MinimumTrackTintColor = opaqueReturnColor;
				});
			}
			return continueTrack;
		}

		public override void CancelTracking(UIEvent uievent)
		{
			base.CancelTracking(uievent);
			if (!PopUpViewAlwaysOn)
			{
				HidePopUpViewAnimated(true);
			}
		}

		public override void EndTracking(UITouch uitouch, UIEvent uievent)
		{
			base.EndTracking(uitouch, uievent);
			if (!PopUpViewAlwaysOn)
			{
				HidePopUpViewAnimated(true);
			}
		}
	}
}

