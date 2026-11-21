// Dark/light-aware styles for buttons, switches, boxes, drop-downs, sliders, scroll bars
// and the side tab list. They use DynamicResource brushes (see ThemeManager), so a theme
// change recolours everything. Parsed from XAML text at startup.
using System.Windows;
using System.Windows.Markup;

namespace BetterClock
{
    public static class ControlStyles
    {
        public static ResourceDictionary Create()
        {
            return (ResourceDictionary)XamlReader.Parse(Xaml);
        }

        private const string Xaml = @"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

  <!-- ============ Button ============ -->
  <Style TargetType='Button'>
    <Setter Property='Foreground' Value='{DynamicResource InkBrush}'/>
    <Setter Property='Background' Value='{DynamicResource ControlBrush}'/>
    <Setter Property='BorderBrush' Value='{DynamicResource LineBrush}'/>
    <Setter Property='Padding' Value='14,6'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='Button'>
          <Grid x:Name='Root' RenderTransformOrigin='0.5,0.5'>
            <Grid.RenderTransform><ScaleTransform ScaleX='1' ScaleY='1'/></Grid.RenderTransform>
            <Border x:Name='Bd' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}'
                    BorderThickness='1' CornerRadius='8' Padding='{TemplateBinding Padding}'>
              <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
            </Border>
            <Border x:Name='Fx' Background='{DynamicResource AccentBrush}' CornerRadius='8' Opacity='0' IsHitTestVisible='False'/>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Trigger.EnterActions><BeginStoryboard><Storyboard>
                <DoubleAnimation Storyboard.TargetName='Fx' Storyboard.TargetProperty='Opacity' To='0.25' Duration='0:0:0.12'/>
              </Storyboard></BeginStoryboard></Trigger.EnterActions>
              <Trigger.ExitActions><BeginStoryboard><Storyboard>
                <DoubleAnimation Storyboard.TargetName='Fx' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.2'/>
              </Storyboard></BeginStoryboard></Trigger.ExitActions>
            </Trigger>
            <Trigger Property='IsPressed' Value='True'>
              <Trigger.EnterActions><BeginStoryboard><Storyboard>
                <DoubleAnimation Storyboard.TargetName='Root' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='0.95' Duration='0:0:0.07'/>
                <DoubleAnimation Storyboard.TargetName='Root' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='0.95' Duration='0:0:0.07'/>
              </Storyboard></BeginStoryboard></Trigger.EnterActions>
              <Trigger.ExitActions><BeginStoryboard><Storyboard>
                <DoubleAnimation Storyboard.TargetName='Root' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleX)' To='1' Duration='0:0:0.12'/>
                <DoubleAnimation Storyboard.TargetName='Root' Storyboard.TargetProperty='(UIElement.RenderTransform).(ScaleTransform.ScaleY)' To='1' Duration='0:0:0.12'/>
              </Storyboard></BeginStoryboard></Trigger.ExitActions>
            </Trigger>
            <Trigger Property='IsEnabled' Value='False'>
              <Setter TargetName='Root' Property='Opacity' Value='0.45'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- ============ ToggleButton (position grid) ============ -->
  <Style TargetType='ToggleButton'>
    <Setter Property='Foreground' Value='{DynamicResource InkBrush}'/>
    <Setter Property='Background' Value='{DynamicResource ControlBrush}'/>
    <Setter Property='BorderBrush' Value='{DynamicResource LineBrush}'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ToggleButton'>
          <Grid x:Name='Root' RenderTransformOrigin='0.5,0.5'>
            <Grid.RenderTransform><ScaleTransform ScaleX='1' ScaleY='1'/></Grid.RenderTransform>
            <Border x:Name='Bd' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}'
                    BorderThickness='1' CornerRadius='8'>
              <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
            </Border>
            <Border x:Name='On' Background='{DynamicResource AccentBrush}' CornerRadius='8' Opacity='0' IsHitTestVisible='False'/>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Bd' Property='BorderBrush' Value='{DynamicResource AccentBrush}'/>
            </Trigger>
            <Trigger Property='IsChecked' Value='True'>
              <Trigger.EnterActions><BeginStoryboard><Storyboard>
                <DoubleAnimation Storyboard.TargetName='On' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.18'/>
              </Storyboard></BeginStoryboard></Trigger.EnterActions>
              <Trigger.ExitActions><BeginStoryboard><Storyboard>
                <DoubleAnimation Storyboard.TargetName='On' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.18'/>
              </Storyboard></BeginStoryboard></Trigger.ExitActions>
              <Setter Property='Foreground' Value='White'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- ============ CheckBox (switch) ============ -->
  <Style TargetType='CheckBox'>
    <Setter Property='Foreground' Value='{DynamicResource InkBrush}'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='CheckBox'>
          <StackPanel Orientation='Horizontal' Background='Transparent'>
            <Grid Width='40' Height='22' Margin='0,0,12,0' VerticalAlignment='Center'>
              <Border CornerRadius='11' Background='{DynamicResource LineBrush}'/>
              <Border x:Name='OnFill' CornerRadius='11' Background='{DynamicResource AccentBrush}' Opacity='0'/>
              <Ellipse x:Name='Knob' Width='16' Height='16' Fill='White' HorizontalAlignment='Left' Margin='3,0,0,0'>
                <Ellipse.RenderTransform><TranslateTransform X='0'/></Ellipse.RenderTransform>
              </Ellipse>
            </Grid>
            <ContentPresenter VerticalAlignment='Center'/>
          </StackPanel>
          <ControlTemplate.Triggers>
            <Trigger Property='IsChecked' Value='True'>
              <Trigger.EnterActions><BeginStoryboard><Storyboard>
                <DoubleAnimation Storyboard.TargetName='OnFill' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.18'/>
                <DoubleAnimation Storyboard.TargetName='Knob' Storyboard.TargetProperty='(UIElement.RenderTransform).(TranslateTransform.X)' To='18' Duration='0:0:0.18'>
                  <DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction>
                </DoubleAnimation>
              </Storyboard></BeginStoryboard></Trigger.EnterActions>
              <Trigger.ExitActions><BeginStoryboard><Storyboard>
                <DoubleAnimation Storyboard.TargetName='OnFill' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.18'/>
                <DoubleAnimation Storyboard.TargetName='Knob' Storyboard.TargetProperty='(UIElement.RenderTransform).(TranslateTransform.X)' To='0' Duration='0:0:0.18'>
                  <DoubleAnimation.EasingFunction><CubicEase EasingMode='EaseOut'/></DoubleAnimation.EasingFunction>
                </DoubleAnimation>
              </Storyboard></BeginStoryboard></Trigger.ExitActions>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- ============ TextBox ============ -->
  <Style TargetType='TextBox'>
    <Setter Property='Foreground' Value='{DynamicResource InkBrush}'/>
    <Setter Property='CaretBrush' Value='{DynamicResource InkBrush}'/>
    <Setter Property='SelectionBrush' Value='{DynamicResource AccentBrush}'/>
    <Setter Property='Padding' Value='8,4'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='TextBox'>
          <Border x:Name='Bd' Background='{DynamicResource ControlBrush}' BorderBrush='{DynamicResource LineBrush}'
                  BorderThickness='1' CornerRadius='8' Padding='{TemplateBinding Padding}'>
            <ScrollViewer x:Name='PART_ContentHost' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsKeyboardFocused' Value='True'>
              <Setter TargetName='Bd' Property='BorderBrush' Value='{DynamicResource AccentBrush}'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- ============ ComboBox ============ -->
  <Style TargetType='ComboBoxItem'>
    <Setter Property='Foreground' Value='{DynamicResource InkBrush}'/>
    <Setter Property='Padding' Value='10,6'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ComboBoxItem'>
          <Border x:Name='Bd' Background='Transparent' CornerRadius='6' Margin='3,1' Padding='{TemplateBinding Padding}'>
            <ContentPresenter/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsHighlighted' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{DynamicResource AccentSoftBrush}'/>
            </Trigger>
            <Trigger Property='IsSelected' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{DynamicResource AccentSoftBrush}'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType='ComboBox'>
    <Setter Property='Foreground' Value='{DynamicResource InkBrush}'/>
    <Setter Property='Height' Value='32'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ComboBox'>
          <Grid>
            <ToggleButton x:Name='Toggle' Focusable='False' ClickMode='Press'
                          IsChecked='{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}'>
              <ToggleButton.Template>
                <ControlTemplate TargetType='ToggleButton'>
                  <Border x:Name='Bd' Background='{DynamicResource ControlBrush}' BorderBrush='{DynamicResource LineBrush}'
                          BorderThickness='1' CornerRadius='8'>
                    <Path HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,0,12,0'
                          Data='M 0 0 L 4 4 L 8 0' Stroke='{DynamicResource MutedBrush}' StrokeThickness='2'/>
                  </Border>
                  <ControlTemplate.Triggers>
                    <Trigger Property='IsMouseOver' Value='True'>
                      <Setter TargetName='Bd' Property='BorderBrush' Value='{DynamicResource AccentBrush}'/>
                    </Trigger>
                  </ControlTemplate.Triggers>
                </ControlTemplate>
              </ToggleButton.Template>
            </ToggleButton>
            <ContentPresenter x:Name='ContentSite' IsHitTestVisible='False'
                              Content='{TemplateBinding SelectionBoxItem}'
                              ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'
                              Margin='12,0,32,0' VerticalAlignment='Center' HorizontalAlignment='Left'/>
            <Popup x:Name='Popup' Placement='Bottom' IsOpen='{TemplateBinding IsDropDownOpen}'
                   AllowsTransparency='True' Focusable='False' PopupAnimation='Slide'>
              <Grid MinWidth='{TemplateBinding ActualWidth}' MaxHeight='{TemplateBinding MaxDropDownHeight}'>
                <Border Background='{DynamicResource PanelBrush}' BorderBrush='{DynamicResource LineBrush}'
                        BorderThickness='1' CornerRadius='8' Margin='0,4,0,8' Padding='0,4'>
                  <ScrollViewer>
                    <ItemsPresenter KeyboardNavigation.DirectionalNavigation='Contained'/>
                  </ScrollViewer>
                </Border>
              </Grid>
            </Popup>
          </Grid>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- ============ Slider ============ -->
  <Style TargetType='Slider'>
    <Setter Property='Focusable' Value='True'/>
    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='Slider'>
          <Grid Height='24'>
            <Border Height='4' CornerRadius='2' Background='{DynamicResource LineBrush}' VerticalAlignment='Center'/>
            <Track x:Name='PART_Track'>
              <Track.DecreaseRepeatButton>
                <RepeatButton Command='{x:Static Slider.DecreaseLarge}' Focusable='False'>
                  <RepeatButton.Template>
                    <ControlTemplate TargetType='RepeatButton'>
                      <Border Height='4' CornerRadius='2' Background='{DynamicResource AccentBrush}' VerticalAlignment='Center'/>
                    </ControlTemplate>
                  </RepeatButton.Template>
                </RepeatButton>
              </Track.DecreaseRepeatButton>
              <Track.IncreaseRepeatButton>
                <RepeatButton Command='{x:Static Slider.IncreaseLarge}' Focusable='False'>
                  <RepeatButton.Template>
                    <ControlTemplate TargetType='RepeatButton'>
                      <Border Background='Transparent' Height='24'/>
                    </ControlTemplate>
                  </RepeatButton.Template>
                </RepeatButton>
              </Track.IncreaseRepeatButton>
              <Track.Thumb>
                <Thumb Focusable='False'>
                  <Thumb.Template>
                    <ControlTemplate TargetType='Thumb'>
                      <Ellipse Width='18' Height='18' Fill='White' Stroke='{DynamicResource AccentBrush}' StrokeThickness='3'/>
                    </ControlTemplate>
                  </Thumb.Template>
                </Thumb>
              </Track.Thumb>
            </Track>
          </Grid>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- ============ ScrollBar (vertical) ============ -->
  <Style TargetType='ScrollBar'>
    <Setter Property='Width' Value='10'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ScrollBar'>
          <Grid Width='10' Background='Transparent'>
            <Track x:Name='PART_Track' IsDirectionReversed='True'>
              <Track.DecreaseRepeatButton>
                <RepeatButton Command='{x:Static ScrollBar.PageUpCommand}' Focusable='False'>
                  <RepeatButton.Template>
                    <ControlTemplate TargetType='RepeatButton'><Border Background='Transparent'/></ControlTemplate>
                  </RepeatButton.Template>
                </RepeatButton>
              </Track.DecreaseRepeatButton>
              <Track.IncreaseRepeatButton>
                <RepeatButton Command='{x:Static ScrollBar.PageDownCommand}' Focusable='False'>
                  <RepeatButton.Template>
                    <ControlTemplate TargetType='RepeatButton'><Border Background='Transparent'/></ControlTemplate>
                  </RepeatButton.Template>
                </RepeatButton>
              </Track.IncreaseRepeatButton>
              <Track.Thumb>
                <Thumb>
                  <Thumb.Template>
                    <ControlTemplate TargetType='Thumb'>
                      <Border Margin='2,0' CornerRadius='3' Background='{DynamicResource MutedBrush}' Opacity='0.5'/>
                    </ControlTemplate>
                  </Thumb.Template>
                </Thumb>
              </Track.Thumb>
            </Track>
          </Grid>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <!-- ============ Side tab list ============ -->
  <Style TargetType='TabControl'>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='BorderThickness' Value='0'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='TabControl'>
          <Grid>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width='176'/>
              <ColumnDefinition Width='*'/>
            </Grid.ColumnDefinitions>
            <Border Grid.Column='0' Background='{DynamicResource PanelBrush}' CornerRadius='14' Margin='0,0,10,0' Padding='8,12'>
              <StackPanel IsItemsHost='True'/>
            </Border>
            <Border Grid.Column='1' Background='{DynamicResource PanelBrush}' CornerRadius='14'>
              <ContentPresenter ContentSource='SelectedContent'/>
            </Border>
          </Grid>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType='TabItem'>
    <Setter Property='Foreground' Value='{DynamicResource MutedBrush}'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='TabItem'>
          <Grid Margin='0,2'>
            <Border x:Name='Bd' CornerRadius='9' Background='Transparent'/>
            <Border x:Name='Sel' CornerRadius='9' Background='{DynamicResource AccentSoftBrush}' Opacity='0'/>
            <Border x:Name='Bar' Width='4' Height='18' CornerRadius='2' Background='{DynamicResource AccentBrush}'
                    HorizontalAlignment='Left' VerticalAlignment='Center' Margin='4,0,0,0' Opacity='0'/>
            <ContentPresenter ContentSource='Header' Margin='20,10,12,10' VerticalAlignment='Center'/>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter Property='Foreground' Value='{DynamicResource InkBrush}'/>
            </Trigger>
            <Trigger Property='IsSelected' Value='True'>
              <Trigger.EnterActions><BeginStoryboard><Storyboard>
                <DoubleAnimation Storyboard.TargetName='Sel' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.2'/>
                <DoubleAnimation Storyboard.TargetName='Bar' Storyboard.TargetProperty='Opacity' To='1' Duration='0:0:0.2'/>
              </Storyboard></BeginStoryboard></Trigger.EnterActions>
              <Trigger.ExitActions><BeginStoryboard><Storyboard>
                <DoubleAnimation Storyboard.TargetName='Sel' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.2'/>
                <DoubleAnimation Storyboard.TargetName='Bar' Storyboard.TargetProperty='Opacity' To='0' Duration='0:0:0.2'/>
              </Storyboard></BeginStoryboard></Trigger.ExitActions>
              <Setter Property='Foreground' Value='{DynamicResource InkBrush}'/>
              <Setter Property='FontWeight' Value='SemiBold'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

</ResourceDictionary>";
    }
}
