
// ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// This UIView subclass is used internally by ASValueTrackingSlider
// The public API is declared in ASValueTrackingSlider.h
// ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


using System;
using UIKit;
using Foundation;
using CoreGraphics;
using CoreAnimation;
using ObjCRuntime;
using System.Collections.Generic;


namespace ASValueTrackingSlider.ValueTrackingSlider
{
	public static class Extensions
	{
		public static void AnimateKey(this CALayer layer, NSString animationName, NSObject fromValue, NSObject toValue, Action<CABasicAnimation> block)
		{
			layer.SetValueForKey(toValue, animationName);
			var anim = CABasicAnimation.FromKeyPath(animationName);
			anim.From = fromValue ?? layer.PresentationLayer.ValueForKey(animationName);
			anim.To = toValue;
			block?.Invoke(anim);
			layer.AddAnimation(anim, animationName);
		}
	}


	public interface IASValuePopUpViewDelegate
	{
		nfloat CurrentValueOffset { get; }  // expects value in the range 0.0 - 1.0
		void ColorDidUpdate(UIColor opaqueColor);
	}

	public class ValuePopUpView : UIView
	{
		public const string SliderFillColorAnim = "fillColor";

		public IASValuePopUpViewDelegate Delegate { get; set; }

		private bool shouldAnimate;
		private double animDuration;
		private NSMutableAttributedString attributedString;
		private CAShapeLayer pathLayer;
		private CATextLayer textLayer;
		private nfloat arrowCenterOffset;

		// never actually visible, its purpose is to interpolate color values for the popUpView color animation
		// using shape layer because it has a 'fillColor' property which is consistent with _backgroundLayer
		private CAShapeLayer colorAnimLayer;

		public ValuePopUpView(CGRect frame) : base(frame)
		{
			shouldAnimate = false;
			Layer.AnchorPoint = new CGPoint(0.5, 1);

			UserInteractionEnabled = false;
			pathLayer = (CAShapeLayer)this.Layer;

			cornerRadius = 4.0f;
			ArrowLength = 13.0f;
			WidthPaddingFactor = 1.15f;
			HeightPaddingFactor = 1.1f;

			textLayer = new CATextLayer();
			textLayer.AlignmentMode = CATextLayer.AlignmentCenter;
			textLayer.AnchorPoint = new CGPoint(0, 0);
			textLayer.ContentsScale = UIScreen.MainScreen.Scale;
			textLayer.Actions = NSDictionary.FromObjectAndKey(NSNull.Null, new NSString("contents"));

			colorAnimLayer = new CAShapeLayer();

			Layer.AddSublayer(colorAnimLayer);
			Layer.AddSublayer(textLayer);

			attributedString = new NSMutableAttributedString(" ", new NSDictionary());
		}

		public ValuePopUpView(IntPtr handle) : base(handle)
		{

		}

		private nfloat cornerRadius;
		public nfloat CornerRadius
		{
			get { return cornerRadius; }
			set
			{
				if (cornerRadius == value) return;
				cornerRadius = value;
				pathLayer.Path = PathForRect(this.Bounds, arrowCenterOffset)?.CGPath;
			}
		}

		public nfloat ArrowLength { get; set; }
		public nfloat WidthPaddingFactor { get; set; }
		public nfloat HeightPaddingFactor { get; set; }


		public UIColor Color
		{
			get
			{
				var sl = pathLayer.PresentationLayer as CAShapeLayer;
				return UIColor.FromCGColor(sl.FillColor);
			}
			set
			{
				pathLayer.FillColor = value.CGColor;
				colorAnimLayer.RemoveAnimation(SliderFillColorAnim); // single color, no animation required
			}
		}


		public UIColor OpaqueColor
		{
			get
			{
				var sl = colorAnimLayer.PresentationLayer as CAShapeLayer;
				var fc = sl == null ? pathLayer.FillColor : sl.FillColor;
				return OpaqueUIColorFromCGColor(fc);
			}
		}


		public void SetTextColor(UIColor color)
		{
			textLayer.ForegroundColor = color.CGColor;
		}

		public void SetFont(UIFont font)
		{
			attributedString.AddAttribute(UIStringAttributeKey.Font, font, new NSRange(0, attributedString.Length));
			textLayer.SetFont(font.Name);
			textLayer.FontSize = font.PointSize;
		}

		public void SetText(string text)
		{
			attributedString.MutableString.SetString(new NSString(text));
			textLayer.String = text;
		}

		// set up an animation, but prevent it from running automatically
		// the animation progress will be adjusted manually
		public void SetAnimatedColors(UIColor[] animatedColors, NSNumber[] keyTimes)
		{
			var cgColors = new List<NSObject>();
			foreach (var col in animatedColors)
			{
				cgColors.Add(NSObject.FromObject(col.CGColor));
			}

			var colorAnim = CAKeyFrameAnimation.FromKeyPath(SliderFillColorAnim);
			colorAnim.KeyTimes = keyTimes;
			colorAnim.Values = cgColors.ToArray();
			colorAnim.FillMode = CAFillMode.Both;
			colorAnim.Duration = 1.0;
			colorAnim.Delegate = new AnimationDelegate(this);

			// As the interpolated color values from the presentationLayer are needed immediately
			// the animation must be allowed to start to initialize _colorAnimLayer's presentationLayer
			// hence the speed is set to min value - then set to zero in 'animationDidStart:' delegate method
			colorAnimLayer.Speed = 1.175494351e-38F;  // FLT_MIN
													  //colorAnimLayer.Speed = 0.0000000000000000000000000000000000000117549435f;
			colorAnimLayer.TimeOffset = 0.0;

			colorAnimLayer.AddAnimation(colorAnim, SliderFillColorAnim);
		}

		public void SetAnimationOffset(nfloat animOffset, Action<UIColor> block)
		{
			if (colorAnimLayer.AnimationForKey(SliderFillColorAnim) != null)
			{
				colorAnimLayer.TimeOffset = animOffset;
				pathLayer.FillColor = ((CAShapeLayer)colorAnimLayer.PresentationLayer).FillColor;
				block?.Invoke(OpaqueColor);
			}
		}

		public void SetFrame(CGRect frame, nfloat arrowOffset, string text)
		{
			// only redraw path if either the arrowOffset or popUpView size has changed
			if (arrowOffset != arrowCenterOffset || !(frame.Size == this.Frame.Size))
			{
				pathLayer.Path = PathForRect(frame, arrowOffset).CGPath;
			}
			arrowCenterOffset = arrowOffset;

			nfloat anchorX = 0.5f + (arrowOffset / frame.Width);
			Layer.AnchorPoint = new CGPoint(anchorX, 1);
			Layer.Position = new CGPoint(frame.GetMinX() + frame.Width * anchorX, 0);
			Layer.Bounds = new CGRect(CGPoint.Empty, frame.Size);

			SetText(text);
		}

		// _shouldAnimate = YES; causes 'actionForLayer:' to return an animation for layer property changes
		// call the supplied block, then set _shouldAnimate back to NO
		public void AnimateBlock(Action<double> block)
		{
			shouldAnimate = true;
			animDuration = 0.5f;

			var anim = Layer.AnimationForKey("position");
			if (anim != null)
			{
				double elapsedTime = Math.Min(CAAnimation.CurrentMediaTime() - anim.BeginTime, anim.Duration);
				animDuration = animDuration * elapsedTime / anim.Duration;
			}

			block?.Invoke(animDuration);
			shouldAnimate = false;
		}

		public CGSize PopUpSizeForString(string str)
		{
			attributedString.MutableString.SetString(new NSString(str));
			nfloat w, h;
			w = NMath.Ceiling(attributedString.Size.Width * WidthPaddingFactor);
			h = NMath.Ceiling(attributedString.Size.Height * HeightPaddingFactor + ArrowLength);
			return new CGSize(w, h);
		}

		public void ShowAnimated(bool animated)
		{
			if (!animated)
			{
				Layer.Opacity = 1.0f;
				return;
			}

			CATransaction.Begin();
			{
				// start the transform animation from scale 0.5, or its current value if it's already running
				NSObject fromValue = Layer.AnimationForKey("transform") != null
										 ? Layer.PresentationLayer.ValueForKey(new NSString("transform"))
										 : NSValue.FromCATransform3D(CATransform3D.MakeScale(0.5f, 0.5f, 1));

				Layer.AnimateKey(new NSString("transform"),
											 fromValue,
											 NSValue.FromCATransform3D(CATransform3D.Identity),
											 (animation) =>
											 {
												 animation.Duration = 0.4;
												 animation.TimingFunction = CAMediaTimingFunction.FromControlPoints(0.8f, 2.5f, 0.35f, 0.5f);
											 });
				Layer.AnimateKey(new NSString("opacity"), null, NSObject.FromObject(1.0), (animation) =>
				 {
					 animation.Duration = 0.1;
				 });
			}
			CATransaction.Commit();
		}

		public void HideAnimated(bool animated, Action completion)
		{
			CATransaction.Begin();
			{
				CATransaction.CompletionBlock = () =>
				{
					completion?.Invoke();
					Layer.Transform = CATransform3D.Identity;
				};

				if (animated)
				{
					Layer.AnimateKey(new NSString("transform"), null,
									 NSValue.FromCATransform3D(CATransform3D.MakeScale(0.5f, 0.5f, 1)),
									 (animation) =>
									 {
										 animation.Duration = 0.55;
										 animation.TimingFunction = CAMediaTimingFunction.FromControlPoints(0.1f, -2, 0.3f, 3);
									 });
					Layer.AnimateKey(new NSString("opacity"), null,
									 NSObject.FromObject(0.0), (animation) =>
									 {
										 animation.Duration = 0.75;
									 });
				}
				else // not animated - just set opacity to 0.0
				{
					Layer.Opacity = 0.0f;
				}
			}
			CATransaction.Commit();
		}

		[Export("layerClass")]
		public static Class LayerClass()
		{
			return new Class(typeof(CAShapeLayer));
		}

		// if ivar _shouldAnimate) is YES then return an animation
		// otherwise return NSNull (no animation)
		public override NSObject ActionForLayer(CALayer layer, string eventKey)
		{
			if (shouldAnimate)
			{
				var anim = CABasicAnimation.FromKeyPath(eventKey);
				anim.BeginTime = CAAnimation.CurrentMediaTime();
				anim.TimingFunction = CAMediaTimingFunction.FromName(CAMediaTimingFunction.EaseInEaseOut);
				anim.From = layer.PresentationLayer.ValueForKey(new NSString(eventKey));
				anim.Duration = animDuration;
				return anim;
			}
			else
			{
				return NSNull.Null;
			}
		}

		private UIBezierPath PathForRect(CGRect rect, nfloat arrowOffset)
		{
			if (rect == CGRect.Empty) return null;
			rect = new CGRect(CGPoint.Empty, rect.Size);  // ensure origin is CGPointZero

			// Create rounded rect
			CGRect roundedRect = rect;
			var size = roundedRect.Size;
			size.Height -= ArrowLength;
			roundedRect.Size = size;
			var popUpPath = UIBezierPath.FromRoundedRect(roundedRect, cornerRadius);

			// Create arrow path
			nfloat maxX = roundedRect.GetMaxX();  // prevent arrow from extending beyond this point
			nfloat arrowTipX = rect.GetMidX() + arrowOffset;
			CGPoint tip = new CGPoint(arrowTipX, rect.GetMaxY());

			nfloat arrowLength = roundedRect.Height / 2.0f;
			nfloat x = arrowLength * NMath.Tan(45.0f * NMath.PI / 180);  // x = half the length of the base of the arrow

			var arrowPath = new UIBezierPath();
			arrowPath.MoveTo(tip);
			arrowPath.AddLineTo(new CGPoint(NMath.Max(arrowTipX - x, 0), roundedRect.GetMaxY() - arrowLength));
			arrowPath.AddLineTo(new CGPoint(NMath.Min(arrowTipX + x, maxX), roundedRect.GetMaxY() - arrowLength));
			arrowPath.ClosePath();

			popUpPath.AppendPath(arrowPath);

			return popUpPath;
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			nfloat textHeight = attributedString.Size.Height;
			CGRect textRect = new CGRect(this.Bounds.X,
										 (this.Bounds.Size.Height - ArrowLength - textHeight) / 2.0f,
										  this.Bounds.Size.Width,
										  textHeight);
			textLayer.Frame = textRect.Integral();
		}

		private static UIColor OpaqueUIColorFromCGColor(CGColor col)
		{
			if (col == null) return null;

			nfloat[] components = col.Components;
			UIColor color;
			if (col.Components.Length == 2)
			{
				color = UIColor.FromWhiteAlpha(components[0], 1);
			}
			else
			{
				color = UIColor.FromRGBA(components[0], components[1], components[2], 1);
			}
			return color;
		}



		private class AnimationDelegate : CAAnimationDelegate
		{
			// TODO: weakreference
			private readonly ValuePopUpView popUpView;
			public AnimationDelegate(ValuePopUpView popUpView)
			{
				this.popUpView = popUpView;
			}

			// set the speed to zero to freeze the animation and set the offset to the correct value
			// the animation can now be updated manually by explicity setting its 'timeOffset'
			public override void AnimationStarted(CAAnimation anim)
			{
				popUpView.colorAnimLayer.Speed = 0.0f;
				popUpView.colorAnimLayer.TimeOffset = popUpView.Delegate.CurrentValueOffset;

				popUpView.pathLayer.FillColor = ((CAShapeLayer)popUpView.colorAnimLayer.PresentationLayer).FillColor;
				popUpView.Delegate.ColorDidUpdate(popUpView.OpaqueColor);
			}
		}
	}
}

