// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Function to show lecturer dashboard
function showLecturerDashboard() {
    document.getElementById('lecturer-dashboard').style.display = 'block';
    document.getElementById('coordinator-dashboard').style.display = 'none';
    document.getElementById('manager-dashboard').style.display = 'none';
}

// Function to show coordinator dashboard
function showCoordinatorDashboard() {
    document.getElementById('lecturer-dashboard').style.display = 'none';
    document.getElementById('coordinator-dashboard').style.display = 'block';
    document.getElementById('manager-dashboard').style.display = 'none';
}

// Function to show manager dashboard
function showManagerDashboard() {
    document.getElementById('lecturer-dashboard').style.display = 'none';
    document.getElementById('coordinator-dashboard').style.display = 'none';
    document.getElementById('manager-dashboard').style.display = 'block';
}