   M S B A M L   `   `   `  ����  TcpChatClient(    "TcpChatClient.Views.RegisterWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  회원가입��5   	   $	��270��6   $	��300��6#    � WindowStartupLocation$ CenterScreen=�5   	   .��5      � $��20q�6    � RowDefinitions 5   
   �� 5	      $
��Auto �6   �� 5
      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   .+�5   
   �� $��0��6   $��0,0,0,10q�6"   .+�5      �� $��	아이디��6   �� 5      ��6   �� *
Username+ �� UpdateSourceTrigger$ PropertyChanged=�5      �� 5   
   $��1��6   $��0,0,0,10q�6"   .+�5      �� $��비밀번호��6   G� 5      -   5        5      5      �� 5   
   $��2��6   $��0,0,0,10q�6"   .+�5      �� $��비밀번호 확인��6   G� 5      -   5        5      5      �� 5   
   $��3��6   $��가입하기��6   $	��100��6-   $��30��69   $��Center=�5      ��6.   �� *RegisterCommand+5       5!      