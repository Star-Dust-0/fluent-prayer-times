using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace FluentPrayerTimes;

internal sealed class CalendarPage : StackPanel
{
	private int _y;

	private int _m;

	private readonly TextBlock _title = new TextBlock
	{
		FontSize = 22.0,
		FontWeight = FontWeights.SemiBold,
		HorizontalAlignment = HorizontalAlignment.Center
	};

	private readonly TextBlock _sub = HijriUtil.Sec(new TextBlock
	{
		FontSize = 13.0,
		HorizontalAlignment = HorizontalAlignment.Center
	});

	private readonly Grid _grid = new Grid
	{
		FlowDirection = L.Flow,
		RowSpacing = 2.0,
		ColumnSpacing = 2.0
	};

	private readonly StackPanel _list = new StackPanel
	{
		Spacing = 6.0
	};

	private static string[] Head => new string[7]
	{
		L.T("السبت"),
		L.T("الأحد"),
		L.T("الاثنين"),
		L.T("الثلاثاء"),
		L.T("الأربعاء"),
		L.T("الخميس"),
		L.T("الجمعة")
	};

	public CalendarPage()
	{
		base.Spacing = 10.0;
		(_y, _m, _) = HijriUtil.ToHijri(DateTime.Today);
		base.Children.Add(new TextBlock
		{
			Text = L.T("التقويم الهجري"),
			FontSize = 24.0,
			FontWeight = FontWeights.SemiBold
		});
		Grid grid = new Grid
		{
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = GridLength.Auto
				},
				new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				},
				new ColumnDefinition
				{
					Width = GridLength.Auto
				}
			}
		};
		Button button = new Button
		{
			Content = new FontIcon
			{
				Glyph = (L.Rtl ? "\ue76c" : "\ue76b")
			}
		};
		Button button2 = new Button
		{
			Content = new FontIcon
			{
				Glyph = (L.Rtl ? "\ue76b" : "\ue76c")
			}
		};
		ControlIds.Set(button, "CalendarPrevious");
        ControlIds.Set(button2, "CalendarNext");
        ControlIds.Set(_title, "CalendarTitle");
        button.Click += delegate
		{
			Step(-1);
		};
		button2.Click += delegate
		{
			Step(1);
		};
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 2.0
		};
		stackPanel.Children.Add(_title);
		stackPanel.Children.Add(_sub);
		Grid.SetColumn(button, 0);
		Grid.SetColumn(stackPanel, 1);
		Grid.SetColumn(button2, 2);
		grid.Children.Add(button);
		grid.Children.Add(stackPanel);
		grid.Children.Add(button2);
		Button button3 = new Button
		{
			Content = L.T("اليوم"),
			HorizontalAlignment = HorizontalAlignment.Center
		};
		ControlIds.Set(button3, "CalendarToday");
        button3.Click += delegate
		{
			(_y, _m, _) = HijriUtil.ToHijri(DateTime.Today);
			Render();
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 8.0
		};
		stackPanel2.Children.Add(grid);
		stackPanel2.Children.Add(_grid);
		base.Children.Add(HijriUtil.Card(stackPanel2, new Thickness(8.0, 12.0, 8.0, 12.0)));
		base.Children.Add(button3);
		StackPanel stackPanel3 = new StackPanel
		{
			Spacing = 8.0
		};
		stackPanel3.Children.Add(new TextBlock
		{
			Text = L.T("المناسبات في هذا الشهر"),
			FontSize = 16.0,
			FontWeight = FontWeights.SemiBold
		});
		stackPanel3.Children.Add(_list);
		base.Children.Add(HijriUtil.Card(stackPanel3));
		Render();
	}

	private void Step(int d)
	{
		_m += d;
		if (_m > 12)
		{
			_m = 1;
			_y++;
		}
		else if (_m < 1)
		{
			_m = 12;
			_y--;
		}
		_y = Math.Clamp(_y, HijriUtil.MinYear, HijriUtil.MaxYear);
		Render();
	}

	private void Render()
	{
		DateTime dateTime = HijriUtil.ToGreg(_y, _m, 1);
		int daysInMonth = HijriUtil.Cal.GetDaysInMonth(_y, _m);
		DateTime dateTime2 = dateTime.AddDays(daysInMonth - 1);
		_title.Text = HijriUtil.HMonths[_m - 1] + " " + _y;
		_sub.Text = ((dateTime.Month == dateTime2.Month) ? (HijriUtil.GMonths[dateTime.Month - 1] + " " + dateTime.Year) : (HijriUtil.GMonths[dateTime.Month - 1] + " - " + HijriUtil.GMonths[dateTime2.Month - 1] + " " + dateTime2.Year));
		_grid.Children.Clear();
		_grid.RowDefinitions.Clear();
		_grid.ColumnDefinitions.Clear();
		for (int i = 0; i < 7; i++)
		{
			_grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
		}
		int num = (int)(dateTime.DayOfWeek + 1) % 7;
		int num2 = (num + daysInMonth + 6) / 7;
		for (int j = 0; j < num2 + 1; j++)
		{
			_grid.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
		}
		for (int k = 0; k < 7; k++)
		{
			TextBlock textBlock = HijriUtil.Sec(new TextBlock
			{
				Text = Head[k],
				FontSize = 11.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
			});
			Grid.SetColumn(textBlock, k);
			Grid.SetRow(textBlock, 0);
			_grid.Children.Add(textBlock);
		}
		SolidColorBrush solidColorBrush = new SolidColorBrush(HijriUtil.Sky);
		DateTime today = DateTime.Today;
		for (int l = 1; l <= daysInMonth; l++)
		{
			DateTime dateTime3 = dateTime.AddDays(l - 1);
			int num3 = num + l - 1;
			bool flag = dateTime3.Date == today;
			bool flag2 = HijriUtil.Events(_m, l).Count > 0;
			StackPanel stackPanel = new StackPanel
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Spacing = 0.0
			};
			stackPanel.Children.Add(new TextBlock
			{
				Text = l.ToString(),
				FontSize = 15.0,
				FontWeight = (flag ? FontWeights.Bold : FontWeights.Normal),
				HorizontalAlignment = HorizontalAlignment.Center
			});
			stackPanel.Children.Add(HijriUtil.Sec(new TextBlock
			{
				Text = dateTime3.Day.ToString(),
				FontSize = 10.0,
				HorizontalAlignment = HorizontalAlignment.Center
			}));
			stackPanel.Children.Add(new Ellipse
			{
				Width = 6.0,
				Height = 6.0,
				Margin = new Thickness(0.0, 2.0, 0.0, 0.0),
				HorizontalAlignment = HorizontalAlignment.Center,
				Fill = (flag2 ? solidColorBrush : new SolidColorBrush(Microsoft.UI.Colors.Transparent))
			});
			Border border = HijriUtil.Themed("ControlFillColorDefaultBrush");
			border.Child = stackPanel;
			border.CornerRadius = new CornerRadius(4.0);
			border.Padding = new Thickness(0.0, 3.0, 0.0, 2.0);
			border.MinHeight = 52.0;
			if (flag)
			{
				border.Background = new SolidColorBrush(Color.FromArgb(70, 3, 169, 244));
				border.BorderThickness = new Thickness(1.5);
				border.BorderBrush = solidColorBrush;
			}
			Grid.SetColumn(border, num3 % 7);
			Grid.SetRow(border, 1 + num3 / 7);
			_grid.Children.Add(border);
		}
		_list.Children.Clear();
		bool flag3 = false;
		for (int m = 1; m <= daysInMonth; m++)
		{
			List<string> list = HijriUtil.Events(_m, m);
			DateTime dateTime4 = dateTime.AddDays(m - 1);
			foreach (string item in list)
			{
				flag3 = true;
				Grid grid = new Grid
				{
					ColumnSpacing = 10.0,
					FlowDirection = L.Flow
				};
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(4.0)
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(56.0)
				});
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				Border border2 = new Border
				{
					Background = solidColorBrush,
					CornerRadius = new CornerRadius(2.0)
				};
				StackPanel stackPanel2 = new StackPanel();
				stackPanel2.Children.Add(new TextBlock
				{
					Text = dateTime4.Day.ToString(),
					FontSize = 18.0,
					HorizontalAlignment = HorizontalAlignment.Center
				});
				stackPanel2.Children.Add(HijriUtil.Sec(new TextBlock
				{
					Text = HijriUtil.Days[(int)dateTime4.DayOfWeek],
					FontSize = 11.0,
					HorizontalAlignment = HorizontalAlignment.Center
				}));
				StackPanel stackPanel3 = new StackPanel
				{
					Spacing = 1.0
				};
				stackPanel3.Children.Add(new TextBlock
				{
					Text = item,
					FontSize = 14.0,
					TextWrapping = TextWrapping.Wrap
				});
				stackPanel3.Children.Add(HijriUtil.Sec(new TextBlock
				{
					Text = m + " " + HijriUtil.HMonths[_m - 1],
					FontSize = 12.0
				}));
				Grid.SetColumn(border2, 0);
				Grid.SetColumn(stackPanel2, 1);
				Grid.SetColumn(stackPanel3, 2);
				grid.Children.Add(border2);
				grid.Children.Add(stackPanel2);
				grid.Children.Add(stackPanel3);
				_list.Children.Add(grid);
			}
		}
		if (!flag3)
		{
			_list.Children.Add(HijriUtil.Sec(new TextBlock
			{
				Text = L.T("لا توجد مناسبات في هذا الشهر"),
				FontSize = 13.0
			}));
		}
	}
}
