   M S B A M L   `   `   `  ����  TcpChatClient%    TcpChatClient.Views.LoginWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  	로그인��5   	   $	��300��6   $	��300��6"    � WindowStartupLocation$ CenterScreen=�5   	   .��5      � $��20q�6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5	      $
��Auto �6   �� 5
      $
��Auto �6   �� 5      $
��Auto �6   .+�5   
   �� $��0��6   $��0,0,0,10q�6"   .+�5      �� $��	아이디��6   �� 5      ��6   �� *
Username+ �� UpdateSourceTrigger$ PropertyChanged=�5      �� 5   
   $��1��6   $��0,0,0,10q�6"   .+�5      �� $��비밀번호��6   G� 5      -   5         G�Name$ pwdBox��5      6*   5      �� 5   
   $��2��6   $��	로그인��6   $	��100��6,   $��30��68   $��Center=�5      $��0,10,0,0q�6.   ��5      �� *LoginCommand+�� 5   
   $��3��6   $��회원가입��6   $	��100��6-   $��30��69   $��Center=�5      $��0,10,0,0q�6.   ��5      �� *OpenRegisterCommand+5      5       