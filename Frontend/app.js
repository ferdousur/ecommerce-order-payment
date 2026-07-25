const API_BASE = "http://localhost:5004/api";

// State management
let userProfileId = localStorage.getItem("userProfileId") || "";
let currentUser = localStorage.getItem("currentUser") || "";
let currentCartId = ""; // Dynamic Cart ID storage

document.addEventListener("DOMContentLoaded", () => {
    if (currentUser) {
        document.getElementById("userInfo").innerText = `Hello, ${currentUser}`;
        showSection("products");
    } else {
        showSection("auth");
    }
});

// --- Navigation ---
function showSection(section) {
    document.getElementById("authSection").classList.add("d-none");
    document.getElementById("productsSection").classList.add("d-none");
    document.getElementById("cartSection").classList.add("d-none");

    if (section === "auth") document.getElementById("authSection").classList.remove("d-none");
    if (section === "products") {
        document.getElementById("productsSection").classList.remove("d-none");
        loadProducts();
    }
    if (section === "cart") {
        document.getElementById("cartSection").classList.remove("d-none");
        loadCart();
    }
}

// --- Auth ---
document.getElementById("registerForm").addEventListener("submit", async (e) => {
    e.preventDefault();
    const payload = {
        firstName: document.getElementById("regFirst").value,
        lastName: document.getElementById("regLast").value,
        userName: document.getElementById("regUser").value,
        email: document.getElementById("regEmail").value,
        password: document.getElementById("regPass").value
    };
    
    const res = await fetch(`${API_BASE}/Register`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    });
    
    if (res.ok) {
        alert("Registration successful! Please login.");
        document.querySelector('#authTabs a[href="#login"]').click();
    } else {
        alert("Registration failed! Check console.");
    }
});

document.getElementById("loginForm").addEventListener("submit", async (e) => {
    e.preventDefault();
    const payload = {
        userName: document.getElementById("loginUser").value,
        password: document.getElementById("loginPass").value
    };

    const res = await fetch(`${API_BASE}/Auth`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    });

    if (res.ok) {
        const data = await res.json().catch(() => ({}));
        // Try to get userProfileId from response, else fallback (backend should ideally return this)
        userProfileId = data.userProfileId || localStorage.getItem("userProfileId") || "123e4567-e89b-12d3-a456-426614174000";
        currentUser = payload.userName;
        
        localStorage.setItem("userProfileId", userProfileId);
        localStorage.setItem("currentUser", currentUser);
        
        document.getElementById("userInfo").innerText = `Hello, ${currentUser}`;
        showSection("products");
    } else {
        alert("Login failed! Check credentials.");
    }
});

function logout() {
    localStorage.clear();
    location.reload();
}

// --- Products ---
async function loadProducts() {
    try {
        const res = await fetch(`${API_BASE}/products`);
        const products = await res.json().catch(() => []);
        const container = document.getElementById("productsList");
        container.innerHTML = "";

        if (products.length === 0) {
            container.innerHTML = "<p class='text-muted'>No products found.</p>";
            return;
        }

        products.forEach(p => {
            container.innerHTML += `
                <div class="col-md-4 mb-3">
                    <div class="card h-100 shadow-sm">
                        <div class="card-body">
                            <h5 class="card-title">${p.name}</h5>
                            <p class="card-text text-muted small">${p.description || "No description"}</p>
                            <p class="fw-bold text-primary">Price: $${p.price}</p>
                            <button class="btn btn-primary btn-sm w-100" onclick="addToCart('${p.id}')">Add to Cart</button>
                        </div>
                    </div>
                </div>
            `;
        });
    } catch (err) {
        console.error("Load products error:", err);
    }
}

async function addToCart(productId) {
    const payload = {
        userProfileId: userProfileId,
        productId: productId,
        quantity: 1
    };

    try {
        const res = await fetch(`${API_BASE}/carts/add`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            alert("Added to cart successfully!");
        } else {
            alert("Failed to add to cart.");
        }
    } catch (err) {
        console.error("Add to cart error:", err);
    }
}

// --- Cart (Dynamic Cart ID Extraction) ---
async function loadCart() {
    try {
        const res = await fetch(`${API_BASE}/carts/${userProfileId}`);
        if (!res.ok) throw new Error("Failed to fetch cart");
        
        const data = await res.json();
        const container = document.getElementById("cartList");
        container.innerHTML = "";
        currentCartId = ""; // Reset

        // Robust Cart ID extraction (Handles Object or Array response from backend)
        if (data && data.id) currentCartId = data.id;
        else if (data && data.cartId) currentCartId = data.cartId;
        else if (Array.isArray(data) && data.length > 0) {
            currentCartId = data[0].cartId || data[0].id || "";
        }

        if (!currentCartId) {
            container.innerHTML = `<div class="list-group-item text-danger">Cart is empty or invalid format.</div>`;
            document.getElementById("btnCheckout").disabled = true;
            return;
        }

        // Render Cart Items (Adjust property names based on your exact backend response)
        const items = Array.isArray(data) ? data : (data.items || []);
        if (items.length === 0) {
            container.innerHTML = `<div class="list-group-item">Cart is empty.</div>`;
            document.getElementById("btnCheckout").disabled = true;
        } else {
            document.getElementById("btnCheckout").disabled = false;
            items.forEach(item => {
                container.innerHTML += `
                    <div class="list-group-item d-flex justify-content-between align-items-center">
                        <div>
                            <strong>Product ID:</strong> ${item.productId || item.id}<br>
                            <small class="text-muted">Quantity: ${item.quantity}</small>
                        </div>
                    </div>
                `;
            });
        }
    } catch (err) {
        console.error("Load cart error:", err);
        alert("Error loading cart. Please try again.");
    }
}

// --- Checkout & Payment Flow ---
async function processCheckout() {
    if (!currentCartId) {
        alert("Cart ID is missing! Please refresh the cart.");
        return;
    }

    const shippingAddress = document.getElementById("shippingAddress").value.trim();
    const paymentProvider = parseInt(document.getElementById("paymentProvider").value); // 0 = Stripe, 1 = bKash

    if (!shippingAddress) {
        alert("Please enter a shipping address.");
        return;
    }

    const payload = {
        userProfileId: userProfileId,
        cartId: currentCartId,
        paymentProvider: paymentProvider,
        shippingAddress: shippingAddress
    };

    try {
        // Step 1: Create Order
        const res = await fetch(`${API_BASE}/Orders/checkout`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            const orderData = await res.json().catch(() => ({}));
            alert("Order placed successfully!");
            
            // Close modal
            const modalEl = document.getElementById("checkoutModal");
            const modal = bootstrap.Modal.getInstance(modalEl);
            modal.hide();

            // Step 2: If bKash (1) is selected, trigger bKash payment creation
            if (paymentProvider === 1) {
                // Assuming backend returns orderId and amount in checkout response
                const orderId = orderData.orderId || orderData.id;
                const amount = orderData.amount || 0; 
                
                if (orderId) {
                    initiateBkashPayment(orderId, amount);
                } else {
                    alert("Order created, but Order ID missing for bKash payment. Please check backend response.");
                    showSection("products");
                }
            } else {
                // For Stripe (0), you can redirect to Stripe checkout URL if backend provides it
                showSection("products");
            }
        } else {
            const errText = await res.text();
            alert(`Checkout failed: ${errText}`);
        }
    } catch (err) {
        console.error("Checkout error:", err);
        alert("Checkout error: " + err.message);
    }
}

async function initiateBkashPayment(orderId, amount) {
    try {
        const res = await fetch(`${API_BASE}/Payments/bkash/create`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ orderId: orderId, amount: amount })
        });

        if (res.ok) {
            const bkashData = await res.json();
            alert("bKash payment initiated! Redirecting...");
            // If backend returns a bKash redirect URL, use it:
            if (bkashData.bkashURL) {
                window.location.href = bkashData.bkashURL;
            }
        } else {
            alert("bKash payment initiation failed!");
        }
    } catch (err) {
        console.error("bKash payment error:", err);
    }
}