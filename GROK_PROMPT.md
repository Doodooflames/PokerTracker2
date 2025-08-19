# WPF XAML Grid Spacing Issue - Need Expert Help

## Problem Summary
I have a WPF player card layout where two sections (Database Info and Session Activity) are supposed to be compressed horizontally with minimal spacing between them, but the spacing remains wide despite multiple attempts to fix it.

## Current Layout Structure
```xml
<!-- Compact Info Row -->
<Grid Grid.Row="2">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="2"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    
    <!-- Database Info -->
    <Border Grid.Column="0" Background="#40000000" CornerRadius="4" Padding="8" Margin="0,0,0,0">
        <StackPanel>
            <TextBlock Text="Database Info" FontSize="9" FontWeight="SemiBold" Foreground="#CCCCCC" Margin="0,0,0,3"/>
            <TextBlock Text="First Added: N/A" FontSize="8" Foreground="#AAAAAA"/>
            <TextBlock Text="{Binding LastActivityTime, StringFormat='Last: 'MM/dd HH:mm}" FontSize="8" Foreground="#AAAAAA" Margin="0,1,0,0"/>
        </StackPanel>
    </Border>
    
    <!-- Session Activity -->
    <Border Grid.Column="2" Background="#40000000" CornerRadius="4" Padding="8" Margin="0,0,0,0">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid Grid.Row="0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <TextBlock Grid.Column="0" Text="Session Activity" FontSize="9" FontWeight="SemiBold" Foreground="#CCCCCC"/>
                <ui:Button Grid.Column="1" Content="⋯" Width="16" Height="16" FontSize="8" 
                          Background="Transparent" BorderBrush="#60FFFFFF" BorderThickness="1" Foreground="#CCCCCC"
                          ToolTip="Expand activity log" Click="ExpandActivityLog_Click" Tag="{Binding Name}"/>
            </Grid>
            <StackPanel Grid.Row="1" Margin="0,3,0,0">
                <TextBlock Text="• Initial: $220 at 09:35" FontSize="8" Foreground="#BBBBBB" TextTrimming="CharacterEllipsis"/>
                <TextBlock Text="• No other transactions" FontSize="8" Foreground="#BBBBBB" TextTrimming="CharacterEllipsis"/>
            </StackPanel>
        </Grid>
    </Border>
</Grid>
```

## What I've Tried
1. **Margin adjustments**: Changed from `Margin="0,0,4,0"` and `Margin="4,0,0,0"` to `Margin="0,0,0,0"`
2. **3-column Grid**: Added a 2px fixed-width spacer column between the sections
3. **Padding reduction**: Reduced Border padding from 8px to 6px
4. **Various column width configurations**: Tried different Width values

## Expected Result
The two sections should be very close together with only 2px spacing between them, creating a compressed horizontal layout.

## Actual Result
The spacing between the sections remains wide/unchanged, as if there's some inherent Grid behavior or WPF default that's overriding my spacing attempts.

## Context
- This is part of a larger player card layout in a poker tracking application
- The goal is to reclaim horizontal space for a future line graph implementation
- The parent Border has `Padding="12"` and the overall card structure is working fine
- Only the horizontal compression between these two bottom sections is failing

## Question
What WPF/XAML technique should I use to achieve true horizontal compression between these two Border elements? Is there a fundamental Grid behavior I'm missing, or should I use a different layout approach entirely (StackPanel, DockPanel, etc.)?

## Additional Info
- Using .NET 8 WPF
- The ui:Button is a custom button control
- Data binding is working correctly
- No explicit styles are applied to the Grid or Border elements
