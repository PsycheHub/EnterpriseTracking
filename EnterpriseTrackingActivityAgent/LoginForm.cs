namespace EnterpriseTrackingActivityAgent
{
    public partial class LoginForm : Form
    {
        public string Username => txtUsername.Text.Trim();
        public string Password => txtPassword.Text;
        public string? Voucher => string.IsNullOrWhiteSpace(txtVoucher.Text) ? null : txtVoucher.Text.Trim();

        private readonly TextBox txtUsername;
        private readonly TextBox txtPassword;
        private readonly TextBox txtVoucher;
        private readonly Button btnLogin;
        private readonly Label lblError;

        public LoginForm()
        {
            Text = "Enterprise Activity Agent – Sign In";
            Size = new Size(380, 310);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;

            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            Controls.Add(panel);

            int y = 20;
            panel.Controls.Add(MakeLabel("Username:", y));
            txtUsername = MakeTextBox(y + 22); panel.Controls.Add(txtUsername);
            y += 60;

            panel.Controls.Add(MakeLabel("Password:", y));
            txtPassword = MakeTextBox(y + 22, isPassword: true); panel.Controls.Add(txtPassword);
            y += 60;

            panel.Controls.Add(MakeLabel("Voucher (if required):", y));
            txtVoucher = MakeTextBox(y + 22); panel.Controls.Add(txtVoucher);
            y += 60;

            lblError = new Label
            {
                Text = string.Empty,
                ForeColor = Color.Red,
                AutoSize = false,
                Size = new Size(320, 18),
                Location = new Point(20, y),
                Font = new Font("Segoe UI", 8.5f)
            };
            panel.Controls.Add(lblError);
            y += 24;

            btnLogin = new Button
            {
                Text = "Sign In",
                Size = new Size(320, 34),
                Location = new Point(20, y),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnLogin.Click += BtnLogin_Click;
            panel.Controls.Add(btnLogin);

            AcceptButton = btnLogin;
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            lblError.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                lblError.Text = "Username is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                lblError.Text = "Password is required.";
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        public void ShowError(string message)
        {
            lblError.Text = message;
        }

        private static Label MakeLabel(string text, int y) =>
            new Label
            {
                Text = text,
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f)
            };

        private static TextBox MakeTextBox(int y, bool isPassword = false) =>
            new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(320, 24),
                UseSystemPasswordChar = isPassword,
                Font = new Font("Segoe UI", 9.5f)
            };
    }
}
