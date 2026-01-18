// Simple authorization module
(function($) {
    'use strict';

    var SimpleAuth = {
        token: null,
        isAuthenticated: false,
        inactivityTimeout: 10 * 60 * 1000, // 10 minutes in milliseconds
        inactivityTimer: null,
        usersCache: null, // Cache for users list
        modalShown: false, // Flag to prevent repeated modal shows

        init: function() {
            this.token = sessionStorage.getItem('pgad_token');
            if (this.token) {
                this.isAuthenticated = true;
                this.showAuthUI();
                this.startInactivityTimer();
            }

            // Create login modal HTML
            this.createLoginModal();

            // Show login modal on first visit if not authenticated
            if (!this.isAuthenticated && !this.modalShown) {
                this.showLoginModal();
                this.modalShown = true;
            }

            // Listen for Ctrl+Shift+L
            $(document).on('keydown', function(e) {
                if (e.ctrlKey && e.shiftKey && e.keyCode === 76) {
                    e.preventDefault();
                    if (!SimpleAuth.isAuthenticated) {
                        SimpleAuth.showLoginModal();
                    }
                }
            });

            // Track user activity to reset inactivity timer
            $(document).on('mousedown keydown click', function() {
                if (SimpleAuth.isAuthenticated) {
                    SimpleAuth.resetInactivityTimer();
                }
            });

            // Global AJAX error handler for 401 Unauthorized
            $(document).ajaxError(function(event, jqxhr, settings, thrownError) {
                if (jqxhr.status === 401) {
                    console.log('Received 401 Unauthorized - auto logout');
                    SimpleAuth.handleUnauthorized();
                }
            });
        },

        handleUnauthorized: function() {
            // Clear authentication state
            this.token = null;
            this.isAuthenticated = false;
            sessionStorage.removeItem('pgad_token');
            this.hideAuthUI();
            
            // Stop inactivity timer
            if (this.inactivityTimer) {
                clearTimeout(this.inactivityTimer);
                this.inactivityTimer = null;
            }
            
            // Show login modal
            this.showLoginModal();
        },

        startInactivityTimer: function() {
            this.inactivityTimer = setTimeout(function() {
                if (SimpleAuth.isAuthenticated) {
                    console.log('User inactive for 10 minutes. Logging out...');
                    SimpleAuth.logout();
                }
            }, this.inactivityTimeout);
        },

        resetInactivityTimer: function() {
            // Clear existing timer
            if (this.inactivityTimer) {
                clearTimeout(this.inactivityTimer);
            }
            // Start a new timer
            if (this.isAuthenticated) {
                this.startInactivityTimer();
            }
        },

        createLoginModal: function() {
            var modalHtml = `
                <div id="pgad-login-modal" class="pgad-login-modal">
                    <div class="pgad-login-container">
                        <div class="pgad-login-header">
                            <h2>Admin Login</h2>
                            <p>Sempro Technologies</p>
                        </div>
                        <form id="pgad-login-form">
                            <div class="pgad-form-group">
                                <label for="pgad-user-select">Select User</label>
                                <select id="pgad-user-select" name="user" class="pgad-user-select">
                                    <option value="">Loading users...</option>
                                </select>
                            </div>
                            <div class="pgad-form-group">
                                <label for="pgad-password">Password</label>
                                <input type="password" id="pgad-password" name="password" placeholder="Enter password" required autocomplete="current-password" />
                            </div>
                            <div id="pgad-login-error" class="pgad-login-error"></div>
                            <div class="pgad-form-actions">
                                <button type="submit" class="pgad-btn-login">Login</button>
                                <button type="button" class="pgad-btn-cancel" onclick="SimpleAuth.closeLoginModal()">Cancel</button>
                            </div>
                        </form>
                    </div>
                </div>
            `;
            $('body').append(modalHtml);
            
            // Load users list
            this.loadUsers();
            
            $('#pgad-login-form').on('submit', function(e) {
                e.preventDefault();
                SimpleAuth.login($('#pgad-user-select').val(), $('#pgad-password').val());
            });
        },

        loadUsers: function() {
            // Return if users are already cached
            if (this.usersCache) {
                this.populateUserSelect(this.usersCache);
                return;
            }
            
            $.ajax({
                url: '/api/auth/users',
                method: 'GET'
            })
            .done(function(users) {
                SimpleAuth.usersCache = users; // Cache the users
                SimpleAuth.populateUserSelect(users);
            })
            .fail(function() {
                $('#pgad-user-select').html('<option value="">Failed to load users</option>');
            });
        },
        
        populateUserSelect: function(users) {
            var select = $('#pgad-user-select');
            select.empty();
            select.append('<option value="">-- Select a user --</option>');
            users.forEach(function(user) {
                select.append('<option value="' + user + '">' + user + '</option>');
            });
            // Select first user by default
            if (users.length > 0) {
                select.val(users[0]);
            }
        },

        showLoginModal: function() {
            $('#pgad-login-modal').addClass('pgad-active');
            $('#pgad-password').focus();
        },

        closeLoginModal: function() {
            $('#pgad-login-modal').removeClass('pgad-active');
            $('#pgad-login-form')[0].reset();
            $('#pgad-login-error').text('').hide();
        },

        login: function(username, password) {
            $.ajax({
                url: '/api/auth/login',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ username: username, password: password })
            })
            .done(function(data) {
                if (data.success && data.token) {
                    SimpleAuth.token = data.token;
                    SimpleAuth.isAuthenticated = true;
                    sessionStorage.setItem('pgad_token', SimpleAuth.token);
                    SimpleAuth.closeLoginModal();
                    SimpleAuth.showAuthUI();
                    SimpleAuth.startInactivityTimer();
                    console.log('Authorized token: ' + SimpleAuth.token);
                    
                    // Trigger custom event for login success
                    $(document).trigger('loginSuccess');
                    
                    // Refresh the page after successful login
                    setTimeout(function() {
                        location.reload();
                    }, 300);
                } else {
                    $('#pgad-login-error').text('Login failed').show();
                }
            })
            .fail(function() {
                $('#pgad-login-error').text('Invalid username or password').show();
            });
        },

        logout: function() {
            // Clear inactivity timer
            if (this.inactivityTimer) {
                clearTimeout(this.inactivityTimer);
                this.inactivityTimer = null;
            }

            $.ajax({
                url: '/api/auth/logout',
                method: 'POST',
                headers: {
                    'X-Auth-Token': this.token
                }
            })
            .always(function() {
                SimpleAuth.token = null;
                SimpleAuth.isAuthenticated = false;
                sessionStorage.removeItem('pgad_token');
                SimpleAuth.hideAuthUI();
                // Refresh the page after logout
                setTimeout(function() {
                    location.reload();
                }, 300);
            });
        },

        showAuthUI: function() {
            $('body').append(
                '<div id="pgad-auth-badge" class="pgad-auth-badge">' +
                '<span class="pgad-badge-dot"></span> Authorized' +
                '<button id="pgad-logout-btn" class="pgad-logout-btn">Logout</button>' +
                '</div>'
            );
            $('#pgad-logout-btn').on('click', function() {
                SimpleAuth.logout();
            });
        },

        hideAuthUI: function() {
            $('#pgad-auth-badge').fadeOut(300, function() { $(this).remove(); });
        },

        // Get header for authorized requests
        getAuthHeader: function() {
            return this.token ? { 'X-Auth-Token': this.token } : {};
        }
    };

    $(document).ready(function() {
        SimpleAuth.init();
    });

    window.SimpleAuth = SimpleAuth;

})(jQuery);
