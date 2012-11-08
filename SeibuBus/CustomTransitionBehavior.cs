using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Interactivity;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Reactive;

namespace SmartPDA.Behavior
{
	public class CustomTransitionBehavior : Behavior<PhoneApplicationPage>
	{

		#region 依存関係プロパティ

		public static readonly DependencyProperty BackwardInProperty = DependencyProperty.Register("BackwardIn", typeof(Storyboard), typeof(CustomTransitionBehavior), new PropertyMetadata(null));

		public Storyboard BackwardIn
		{
			get { return (Storyboard)GetValue(BackwardInProperty); }
			set { SetValue(BackwardInProperty, value); }
		}

		public static readonly DependencyProperty ForwardInProperty = DependencyProperty.Register("ForwardIn", typeof(Storyboard), typeof(CustomTransitionBehavior), new PropertyMetadata(null));

		public Storyboard ForwardIn
		{
			get { return (Storyboard)GetValue(ForwardInProperty); }
			set { SetValue(ForwardInProperty, value); }
		}

		public static readonly DependencyProperty BackwardOutProperty = DependencyProperty.Register("BackwardOut", typeof(Storyboard), typeof(CustomTransitionBehavior), new PropertyMetadata(null));

		public Storyboard BackwardOut
		{
			get { return (Storyboard)GetValue(BackwardOutProperty); }
			set { SetValue(BackwardOutProperty, value); }
		}

		public static readonly DependencyProperty ForwardOutProperty = DependencyProperty.Register("ForwardOut", typeof(Storyboard), typeof(CustomTransitionBehavior), new PropertyMetadata(null));

		public Storyboard ForwardOut
		{
			get { return (Storyboard)GetValue(ForwardOutProperty); }
			set { SetValue(ForwardOutProperty, value); }
		}

		#endregion

		#region プロパティ

		/// <summary>
		/// この Behavior の名前(デバッグ用。設定しなくても良い)
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// PhoneApplicationPage.Navigating で cancel するときには、ここも true を設定しておく。
		/// 設定しないと画面遷移アニメーションが動いてしまう。
		/// </summary>
		public bool NavigatingCancel { get; set; }
		
		/// <summary>
		/// この Behavior が設定されているページの URI
		/// </summary>
		Uri thisUri;

		/// <summary>
		/// 次ページに移動するページの URI
		/// </summary>
		string nextUrl;

		#endregion

		#region プロパティ (IDisposable)

		IDisposable layoutUpdated;

		IDisposable loaded;

		IDisposable Navigating;

		IDisposable fo;

		IDisposable bo;

		#endregion

		#region コンストラクタ

		protected override void OnAttached()
		{
			// LayoutUpdated 後に、ForwardIn アニメーションを実行する。
			// このタイミングでないと VisualTree 構築が完了していないため。
			this.layoutUpdated = Observable.FromEvent<EventArgs>(this.AssociatedObject, "LayoutUpdated")
				.Subscribe(n =>
				{
					// NavigationService 取得
					NavigationService service = this.AssociatedObject.NavigationService;
					if (service == null)
						return;

					// ページを開くアニメーションを実行する
					System.Diagnostics.Debug.WriteLine(this.Name + " ForwardIn");

					// アニメーション実行
					if (this.ForwardIn != null)
						this.ForwardIn.Begin();

					// 終了
					this.layoutUpdated.Dispose();
				});
						
			// ページロードが完了したら、ページを閉じる際のアニメーションをバインドする
			// Loaded 後でないと NavigationService が初期化されてないため
			this.loaded = Observable.FromEvent<RoutedEventArgs>(this.AssociatedObject, "Loaded")
				.Subscribe(n =>
				{
					// 自分のページ URI を記録しておく
					if (this.thisUri == null)
					{
						this.thisUri = this.AssociatedObject.NavigationService.CurrentSource;
					}

					// ページ遷移要求が来たらアニメーション開始する
					this.Navigating = Observable.FromEvent<NavigatingCancelEventArgs>(this.AssociatedObject.NavigationService, "Navigating")
						.Where(p =>
						{
							// キャンセルされる予定→何もしない
							if (this.NavigatingCancel)
							{
								this.NavigatingCancel = false;
								return false;
							}

							// 続行
							return true;
						})

						// 自分から他のページを開く
						.Where(p =>
						{
							// NavigationService 取得
							NavigationService service = p.Sender as NavigationService;
							if (service == null)
								return false;

							System.Diagnostics.Debug.WriteLine(this.Name + " From : " + service.Source + " " + p.EventArgs.Uri + " to : " + p.EventArgs.NavigationMode);

							// 自分から他のページを開く
							if (service.Source == this.thisUri && p.EventArgs.NavigationMode == NavigationMode.New)
							{
								System.Diagnostics.Debug.WriteLine(this.Name + " ForwardOut");
								
								// アニメーション実行				
								if (this.ForwardOut != null)
								{
									// ページを閉じるアニメーション
									// まず初回の移動はキャンセルし、閉じるアニメーションをしてから次ページに移動する。
									if (string.IsNullOrEmpty(this.nextUrl))
									{
										// この移動はキャンセル
										p.EventArgs.Cancel = true;

										// 次に移動するページ URI を記録しておく
										this.nextUrl = p.EventArgs.Uri.ToString();

										// アニメーション終了後移動
										this.fo = Observable.FromEvent<EventArgs>(this.ForwardOut, "Completed")
											.Subscribe(a =>
											{
												// 次のページに移動
												this.AssociatedObject.NavigationService.Navigate(new Uri(this.nextUrl, UriKind.Relative));

												// 値を初期化して終了
												this.nextUrl = "";
												fo.Dispose();
											});
										
										// 終了アニメーションを実行
										this.ForwardOut.Begin();
										
										return false;
									}
								}
							}

							return true;
						})

						// 自分から他のページに戻る
						.Where(p =>
						{
							// NavigationService 取得
							NavigationService service = p.Sender as NavigationService;
							if (service == null)
								return false;

							// 自分から他のページに戻る
							if (service.Source == this.thisUri && p.EventArgs.NavigationMode == NavigationMode.Back)
							{
								System.Diagnostics.Debug.WriteLine(this.Name + " BackwardOut");

								// アニメーション実行				
								if (this.BackwardOut != null)
								{
									// この移動はキャンセル
									p.EventArgs.Cancel = true;

									// アニメーション終了後移動
									this.bo = Observable.FromEvent<EventArgs>(this.BackwardOut, "Completed")
										.Subscribe(a =>
										{
											// ページを戻す
											if (this.AssociatedObject.NavigationService.CanGoBack)
												this.AssociatedObject.NavigationService.GoBack();
											bo.Dispose();
										});

									// 終了アニメーションを実行
									this.BackwardOut.Begin();
								}

								// デタッチする
								this.OnDetaching();

								return false;
							}

							return true;
						})

						// 他のページから自分に戻ってきた
						.Subscribe(p =>
						{
							// NavigationService 取得
							NavigationService service = p.Sender as NavigationService;
							if (service == null)
								return;
							
							// 他のページから自分に戻ってきた
							if (service.Source != this.thisUri && p.EventArgs.Uri == this.thisUri)
							{
								System.Diagnostics.Debug.WriteLine(this.Name + " BackwardIn");

								// アニメーション実行
								if (this.BackwardIn != null)
									this.BackwardIn.Begin();
							}
						});
					
					// 終了
					this.loaded.Dispose();
				});
									
			base.OnAttached();
		}
				
		protected override void OnDetaching()
		{
			if (this.layoutUpdated != null) { this.layoutUpdated.Dispose(); }
			if (this.loaded != null) { this.loaded.Dispose(); }
			if (this.Navigating != null) { this.Navigating.Dispose(); }
			base.OnDetaching();
		}

		#endregion

	}
}
